WITH RecentOrders (CustomerId, OrderId, OrderDate) AS (
    SELECT o.CustomerId AS CustomerId,
           o.OrderId AS OrderId,
           o.OrderDate AS OrderDate
    FROM dbo.Orders AS o
    WHERE o.OrderDate >= '2025-01-01'
),
RankedOrders AS (
    SELECT r.CustomerId AS CustomerId,
           r.OrderId AS OrderId,
           ROW_NUMBER() OVER (
               PARTITION BY r.CustomerId
               ORDER BY r.OrderDate DESC
           ) AS RowNo
    FROM RecentOrders AS r
)
SELECT ro.CustomerId AS CustomerId,
       ro.OrderId AS OrderId
FROM RankedOrders AS ro
WHERE ro.RowNo = 1
ORDER BY ro.CustomerId ASC
OFFSET 0 ROWS FETCH NEXT 50 ROWS ONLY;
