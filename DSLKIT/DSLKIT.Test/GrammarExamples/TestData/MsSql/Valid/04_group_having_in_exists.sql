SELECT o.CustomerId AS CustomerId,
       COUNT(*) AS OrdersCount
FROM sales.Orders AS o
WHERE o.Status IN ('Open', 'Closed')
GROUP BY o.CustomerId
HAVING COUNT(*) > 3;
