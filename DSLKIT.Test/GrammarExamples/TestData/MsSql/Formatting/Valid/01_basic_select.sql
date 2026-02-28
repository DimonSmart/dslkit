select o.CustomerId as CustomerId,
       o.OrderId as OrderId
from dbo.Orders as o
where o.OrderDate >= '2025-01-01'
order by o.CustomerId desc;
