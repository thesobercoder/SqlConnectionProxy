# Introduction 
This is a small library that contains only one class `SqlConnectionProxy` which acts as a static proxy for the `SqlConnection` class. 
This class adds functionality of logging the `Open`, `Close` and `Dispose` methods using the popular `log4net` package.

# Getting Started
1.	Build the project.
2.	Reference the `SqlConnectionProxy.dll` in your desired project.
3.	Make sure that you have `log4net` configured before using this.
4.	Replace all `SqlConnection` instances with `SqlConnectionProxy`.

# Test
Use the following sql snippet to find out the leaking connections

# Note
This class assumes that you configured `log4net` to write logs to a database table named `Log`. If not, please change the query below with your table name.

```
WITH CTE_Log
AS
	(

		SELECT
			[Assembly] = C.Parsed.value('(/root/assembly)[1]', 'nvarchar(max)'),
			[File] =	 C.Parsed.value('(/root/file)[1]', 'nvarchar(max)'),
			[Line] =	 C.Parsed.value('(/root/line)[1]', 'int'),
			l.Level
			FROM
				Log l
				CROSS APPLY (
					SELECT
						CAST(l.Message AS xml)
				) AS C (Parsed)
			WHERE
				l.Level IN ('SqlConnection.Close', 'SqlConnection.Open', 'SqlConnection.Dispose')
	),

CTE_Close
AS
	(
		SELECT
			l.Assembly,
			l.[File],
			l.Line,
			COUNT(1) AS [CloseCount]
			FROM
				CTE_Log l
			WHERE
				l.Level = 'SqlConnection.Close'
			GROUP BY
				l.Assembly,
				l.[File],
				l.Line
	),
CTE_Open
AS
	(
		SELECT
			l.Assembly,
			l.[File],
			l.Line,
			COUNT(1) AS [OpenCount]
			FROM
				CTE_Log l
			WHERE
				l.Level = 'SqlConnection.Open'
			GROUP BY
				l.Assembly,
				l.[File],
				l.Line
	),
CTE_Dispose
AS
	(
		SELECT
			l.Assembly,
			l.[File],
			l.Line,
			COUNT(1) AS [DisposeCount]
			FROM
				CTE_Log l
			WHERE
				l.Level = 'SqlConnection.Dispose'
			GROUP BY
				l.Assembly,
				l.[File],
				l.Line
	),
CTE_Result
AS
	(
		SELECT
			o.Assembly,
			o.[File],
			o.Line,
			o.OpenCount,
			DisposeCount = ISNULL(d.DisposeCount, 0),
			CloseCount =   ISNULL(c.CloseCount, 0),
			[LeakCount] =  o.OpenCount - IIF(ISNULL(c.CloseCount, 0) > ISNULL(d.DisposeCount, 0), ISNULL(c.CloseCount, 0), ISNULL(d.DisposeCount, 0))
			FROM
				CTE_Open o
				LEFT JOIN CTE_Close c ON c.Assembly = o.Assembly
					AND o.[File] = c.[File]
					AND o.Line = c.Line
				LEFT JOIN CTE_Dispose d ON o.Assembly = d.Assembly
					AND o.[File] = d.[File]
					AND o.Line = d.Line
	)
SELECT * FROM CTE_Result WHERE LeakCount > 0
```

# Author
Soham Dasgupta <soham1.dasgupta@gmail.com>
