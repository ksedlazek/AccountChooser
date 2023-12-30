SELECT 
*
from accounts a 
where a.name like 'Chase%'

SELECT 
z.description as Description,
n.RowId,
a.name as Name,
n.full_name as FullName,
round(avg((1.0*y.value_num) / (1.0*y.value_denom)),2) as Amount,
round(sum((1.0*y.value_num) / (1.0*y.value_denom)),2) as Total,
count(*) as Count
FROM 
(
	SELECT s.tx_guid
	from splits s
	join transactions t on t.guid = s.tx_guid
	where s.account_guid in ( '1eb2fa71d493412fbcc249b886dcb791','23ca0ee73f3949dab6e060b74c3f631e','30c030fdf92b4278900702d845e56a3c','4dffbc2ace94439c9e22cd8220ad35f3','a8df8748857e4914868e1bf02272b93b')
) x
JOIN splits y on y.tx_guid = x.tx_guid AND y.account_guid not in ( '1eb2fa71d493412fbcc249b886dcb791','23ca0ee73f3949dab6e060b74c3f631e','30c030fdf92b4278900702d845e56a3c','4dffbc2ace94439c9e22cd8220ad35f3','a8df8748857e4914868e1bf02272b93b')
JOIN accounts a on a.guid = y.account_guid 
JOIN transactions z on z.guid = y.tx_guid 

JOIN
(
	SELECT
	ROW_NUMBER() OVER(ORDER BY a1.name) AS RowId,
	a1.guid,
	COALESCE (a6.name|| ':','') ||
	COALESCE (a5.name|| ':','') ||
	COALESCE (a4.name|| ':','') ||
	COALESCE (a3.name|| ':','') ||
	COALESCE (a2.name|| ':','') ||
	COALESCE (a1.name,'') as full_name
	FROM accounts a1
	left join accounts a2 on a2.guid = a1.parent_guid and a2.name <> 'Root Account'
	left join accounts a3 on a3.guid = a2.parent_guid  and a3.name <> 'Root Account'
	left join accounts a4 on a4.guid = a3.parent_guid  and a4.name <> 'Root Account'
	left join accounts a5 on a5.guid = a4.parent_guid  and a5.name <> 'Root Account'
	left join accounts a6 on a6.guid = a5.parent_guid  and a6.name <> 'Root Account'
	where COALESCE (a6.name, a5.name, a4.name, a3.name, a2.name, a1.name) not in  ( 'Template Root', 'Root Account', 'Income', 'Equity', 'Assets', 'Liabilities', 'Orphan-USD', 'Imbalance-USD' )
) n on n.guid = a.guid

GROUP by z.description, a.name 
ORDER BY 7 desc



SELECT 
z.post_date  as Date,
z.description as Description,
n.RowId,
a.name as Name,
n.full_name as FullName,
y.value_num as Amount,
b.name as AccountName,
CASE
	WHEN a.name = 'Bank Charge' THEN 'Fees & Adjustments'
	WHEN a.name = 'Charity' THEN 'Gifts & Donations'
	WHEN a.name = 'Christmas' THEN 'Shopping'
	WHEN a.name = 'Clothing' THEN 'Shopping'
	WHEN a.name = 'Clubs' THEN 'Health & Wellness'
	WHEN a.name = 'Dining' THEN 'Food & Drink'
	WHEN a.name = 'Fuel' THEN 'Gas'
	WHEN a.name = 'Lodging' THEN 'Travel'
	WHEN a.name = 'Murray' THEN 'Personal'
	WHEN a.name = 'Shopping' THEN 'Shopping'
	WHEN a.name = 'Streaming' THEN 'Bills & Utilities'
	WHEN a.name = 'Technology' THEN 'Shopping'
	WHEN a.name = 'Travel' THEN 'Travel'
	WHEN n.full_name LIKE 'Expenses:Education:%' THEN 'Education'
	WHEN n.full_name LIKE 'Expenses:Insurance:%' THEN 'Bills & Utilities'
	WHEN n.full_name LIKE 'Expenses:Living:Cleaning%' THEN 'Home'
	WHEN n.full_name LIKE 'Expenses:Living:Clothing:%' THEN 'Shopping'
	WHEN n.full_name LIKE 'Expenses:Living:Household%' THEN 'Shopping'
	WHEN n.full_name LIKE 'Expenses:Living:Vacation:Insurance' THEN 'Travel'
	WHEN n.full_name LIKE 'Expenses:Travel%' THEN 'Travel'
	WHEN n.full_name LIKE 'Expenses:Utilities%' THEN 'Bills & Utilities'
	WHEN n.full_name LIKE '%Dining:Vacation' THEN 'Food & Drink'
	WHEN n.full_name LIKE '%Entertainment%' THEN 'Entertainment'
	WHEN n.full_name LIKE '%Groceries%' THEN 'Groceries'
	WHEN n.full_name LIKE '%Home Improvement%' THEN 'Home'
	WHEN n.full_name LIKE '%Lawn Care%' THEN 'Home'
	WHEN n.full_name LIKE '%Medical%' THEN 'Health & Wellness'
	WHEN n.full_name LIKE '%Vacation:Activities' THEN 'Travel'
	ELSE 'Shopping'
END as Category
FROM 
(
	SELECT s.tx_guid, s.account_guid
	from splits s
	join transactions t on t.guid = s.tx_guid
	where s.account_guid in ( '1eb2fa71d493412fbcc249b886dcb791','23ca0ee73f3949dab6e060b74c3f631e','30c030fdf92b4278900702d845e56a3c','4dffbc2ace94439c9e22cd8220ad35f3','a8df8748857e4914868e1bf02272b93b')
) x
JOIN splits y on y.tx_guid = x.tx_guid AND y.account_guid not in ( '1eb2fa71d493412fbcc249b886dcb791','23ca0ee73f3949dab6e060b74c3f631e','30c030fdf92b4278900702d845e56a3c','4dffbc2ace94439c9e22cd8220ad35f3','a8df8748857e4914868e1bf02272b93b')
JOIN accounts a on a.guid = y.account_guid 
join accounts b on b.guid = x.account_guid
JOIN transactions z on z.guid = y.tx_guid 
JOIN
(
	SELECT
	ROW_NUMBER() OVER(ORDER BY a1.name) AS RowId,
	a1.guid,
	COALESCE (a6.name|| ':','') ||
	COALESCE (a5.name|| ':','') ||
	COALESCE (a4.name|| ':','') ||
	COALESCE (a3.name|| ':','') ||
	COALESCE (a2.name|| ':','') ||
	COALESCE (a1.name,'') as full_name
	FROM accounts a1
	left join accounts a2 on a2.guid = a1.parent_guid and a2.name <> 'Root Account'
	left join accounts a3 on a3.guid = a2.parent_guid  and a3.name <> 'Root Account'
	left join accounts a4 on a4.guid = a3.parent_guid  and a4.name <> 'Root Account'
	left join accounts a5 on a5.guid = a4.parent_guid  and a5.name <> 'Root Account'
	left join accounts a6 on a6.guid = a5.parent_guid  and a6.name <> 'Root Account'
	where COALESCE (a6.name, a5.name, a4.name, a3.name, a2.name, a1.name) not in  ( 'Template Root', 'Root Account', 'Income', 'Equity', 'Assets', 'Liabilities', 'Orphan-USD', 'Imbalance-USD' )
) n on n.guid = a.guid
ORDER BY  8 ASC

