SELECT c.CustomerId AS CustomerId,
       c.Name AS CustomerName,
       CASE
           WHEN lo.Amount > 1000 THEN 'VIP'
           WHEN lo.Amount > 500 THEN 'Preferred'
           ELSE 'Regular'
       END AS Segment
FROM dbo.Customers AS c
OUTER APPLY dbo.GetLatestOrder(c.CustomerId) AS lo
LEFT JOIN dbo.Regions AS r ON r.RegionId = c.RegionId
WHERE c.IsActive = 1 AND (r.Code LIKE 'EU%' OR r.Code IS NULL);
