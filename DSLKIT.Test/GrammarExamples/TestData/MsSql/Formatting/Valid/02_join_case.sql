select c.CustomerId as CustomerId,
       case when o.TotalAmount > 100 then 'vip' else 'regular' end as Segment
from dbo.Customers as c
left join dbo.Orders as o on o.CustomerId = c.CustomerId
where o.TotalAmount is null;
