SELECT CompanyName,
       AddressType,
       AddressLine1
FROM Customer
    JOIN CustomerAddress
        ON (Customer.CustomerID = CustomerAddress.CustomerID)
    JOIN Address
        ON (CustomerAddress.AddressID = Address.AddressID)
WHERE CompanyName = 'ACME Corporation';
