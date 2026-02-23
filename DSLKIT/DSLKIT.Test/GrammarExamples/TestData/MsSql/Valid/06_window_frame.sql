SELECT o.CustomerId AS CustomerId,
       o.OrderDate AS OrderDate,
       SUM(o.Amount) OVER (
           PARTITION BY o.CustomerId
           ORDER BY o.OrderDate ASC
           ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
       ) AS RunningTotal
FROM sales.Orders AS o
WHERE o.Amount > 0;
