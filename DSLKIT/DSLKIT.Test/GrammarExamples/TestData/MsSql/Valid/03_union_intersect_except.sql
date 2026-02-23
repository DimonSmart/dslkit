SELECT e.EmployeeId AS EntityId
FROM hr.Employees AS e
WHERE e.IsActive = 1
UNION ALL
SELECT c.ContractorId AS EntityId
FROM hr.Contractors AS c
WHERE c.IsActive = 1
EXCEPT
SELECT b.BlockedId AS EntityId
FROM hr.BlockedEntities AS b
WHERE b.IsDeleted = 0;
