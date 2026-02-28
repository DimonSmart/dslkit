with recent as (
    select top (10) o.OrderId as OrderId
    from dbo.Orders as o
    order by o.OrderDate desc
)
select r.OrderId as OrderId
from recent as r;
