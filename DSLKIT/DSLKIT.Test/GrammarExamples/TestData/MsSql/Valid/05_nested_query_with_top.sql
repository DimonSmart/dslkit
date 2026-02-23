SELECT TOP (10) p.ProductId AS ProductId,
       p.Name AS ProductName,
       q.TotalQty AS TotalQty
FROM catalog.Products AS p
INNER JOIN (
    SELECT od.ProductId AS ProductId,
           SUM(od.Quantity) AS TotalQty
    FROM sales.OrderDetails AS od
    GROUP BY od.ProductId
) AS q ON q.ProductId = p.ProductId
WHERE p.IsActive = 1
ORDER BY q.TotalQty DESC;
