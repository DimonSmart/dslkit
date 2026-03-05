;WITH
params AS (
    SELECT
        CAST(1 AS int)          AS customer_id,
        CAST(50 AS money)       AS min_amount,
        CAST(0 AS bit)          AS flag,
        N'["EU","US","APAC"]'   AS regions_json,
        CAST(SYSUTCDATETIME() AS datetime2(3)) AS now_utc
),
regions AS (
    SELECT CAST(j.[value] AS varchar(10)) AS region_code
    FROM params p
    CROSS APPLY OPENJSON(p.regions_json) AS j
),
eligible_customers AS (
    SELECT c.CustomerId
    FROM dbo.Customers AS c
    WHERE c.IsActive = 1
    INTERSECT
    SELECT o.CustomerId
    FROM dbo.Orders AS o
),
orders_ranked AS (
    SELECT
        o.CustomerId,
        o.Region,
        o.TotalAmount,
        o.OrderDate,

        ROW_NUMBER() OVER (
            PARTITION BY o.CustomerId
            ORDER BY o.OrderDate DESC, o.TotalAmount DESC, o.Region
        ) AS rn,

        SUM(o.TotalAmount) OVER (PARTITION BY o.CustomerId) AS sum_by_customer,
        AVG(CONVERT(decimal(19,4), o.TotalAmount)) OVER (PARTITION BY o.CustomerId) AS avg_by_customer,

        LAG(o.TotalAmount, 1, 0) OVER (
            PARTITION BY o.CustomerId
            ORDER BY o.OrderDate, o.TotalAmount
        ) AS prev_amount,

        LEAD(o.TotalAmount, 1, 0) OVER (
            PARTITION BY o.CustomerId
            ORDER BY o.OrderDate, o.TotalAmount
        ) AS next_amount
    FROM dbo.Orders AS o
    JOIN params AS p
        ON p.customer_id = o.CustomerId
    WHERE o.TotalAmount >= p.min_amount
      AND EXISTS (SELECT 1 FROM regions r WHERE r.region_code = o.Region)
      AND o.Region LIKE '[A-Z]%' ESCAPE '\'
),
orders_top AS (
    SELECT TOP (25) WITH TIES *
    FROM orders_ranked
    ORDER BY TotalAmount DESC, OrderDate DESC
),
region_pivot AS (
    SELECT
        CustomerId,
        ISNULL([EU], 0)   AS cnt_eu,
        ISNULL([US], 0)   AS cnt_us,
        ISNULL([APAC], 0) AS cnt_apac
    FROM (
        SELECT CustomerId, Region, 1 AS one
        FROM orders_top
    ) AS src
    PIVOT (SUM(one) FOR Region IN ([EU], [US], [APAC])) AS pvt
),
grouped AS (
    SELECT
        CustomerId,
        Region,
        GROUPING(Region) AS is_total,
        COUNT(*)         AS orders_cnt,
        SUM(TotalAmount) AS sum_amount,
        MAX(TotalAmount) AS max_amount
    FROM orders_top
    GROUP BY GROUPING SETS ((CustomerId, Region), (CustomerId))
)
SELECT
    c.CustomerId,

    -- keep   spacing
    c.CustomerId /*  keep   spacing */ AS customer_id,

    c.Region COLLATE Latin1_General_100_CI_AI AS region_ci_ai,
    IIF(c.IsActive = 1, 'active', 'inactive') AS status,

    COALESCE(NULLIF(c.Region, ''), 'n/a') AS region_or_na,
    IIF(EXISTS (SELECT 1 FROM regions r WHERE r.region_code = c.Region), 1, 0) AS in_allowed_regions,

    CASE
        WHEN a.max_amount >= 200 THEN 'vip'
        WHEN a.max_amount >= 100 THEN 'pro'
        ELSE IIF(prm.flag = 1, 'vip', 'std')
    END AS segment,

    a.orders_cnt,
    a.sum_amount,
    a.max_amount,
    a.avg_amount,
    a.regions_list,

    p.cnt_eu,
    p.cnt_us,
    p.cnt_apac,

    last_order.last_order_date,
    last_order.last_order_amount,

    JSON_VALUE(prm.regions_json, '$[0]') AS first_region_from_json,
    CONCAT_WS('|', c.Region, CONVERT(varchar(19), prm.now_utc, 120)) AS region_and_now_utc,

    stats.region_stats_json,

    prm.now_utc
FROM dbo.Customers AS c
CROSS JOIN params AS prm
CROSS APPLY (
    SELECT
        COUNT(*) AS orders_cnt,
        SUM(t.TotalAmount) AS sum_amount,
        MAX(t.TotalAmount) AS max_amount,
        AVG(CONVERT(decimal(19,4), t.TotalAmount)) AS avg_amount,
        STRING_AGG(t.Region, ',') WITHIN GROUP (ORDER BY t.Region) AS regions_list
    FROM orders_top AS t
    WHERE t.CustomerId = c.CustomerId
      AND t.TotalAmount BETWEEN prm.min_amount AND 999999
) AS a
OUTER APPLY (
    SELECT TOP (1)
        t.OrderDate   AS last_order_date,
        t.TotalAmount AS last_order_amount
    FROM orders_top AS t
    WHERE t.CustomerId = c.CustomerId
    ORDER BY t.OrderDate DESC, t.TotalAmount DESC
) AS last_order
OUTER APPLY (
    SELECT (
        SELECT
            g.Region,
            g.is_total,
            g.orders_cnt,
            g.sum_amount,
            g.max_amount
        FROM grouped AS g
        WHERE g.CustomerId = c.CustomerId
        ORDER BY g.is_total, g.Region
        FOR JSON PATH
    ) AS region_stats_json
) AS stats
LEFT JOIN region_pivot AS p
    ON p.CustomerId = c.CustomerId
WHERE c.CustomerId = prm.customer_id
  AND c.CustomerId IN (SELECT CustomerId FROM eligible_customers)
  AND (c.Region = 'EU' OR c.Region = 'US')
  AND a.sum_amount >= 0
ORDER BY
    a.max_amount DESC,
    c.CustomerId DESC
OFFSET 0 ROWS FETCH NEXT 50 ROWS ONLY
OPTION (RECOMPILE);