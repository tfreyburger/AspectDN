# AspectDN
AspectDN is a library used to weave advices directly on binary assemblies developed in .net. 
You have only to describe, with a language derived from C#5, the code or types you want to add or change in the target assembly or assemblies (dll or exe).

### short description
With the help of **AspectDN**, you will be able to declare advices, pointcuts and aspects and weave several types of advices:
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;- Chunk of code
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;- Classes, structures, interfaces, enumerations and delegates
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;- Members : fields, property, event, constructor


Pointcuts and jointoints can be according advice kinds:
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;- Assemblies
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;- Types
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;- Fields
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;- Properties
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;- Methods
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;- Exceptions
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;- Events

Depending on the advice, the corresponding join point is applied:
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;- before, after or around add, remove, get, set or call method
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;- before, after or around a body method, property or event
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;- before, after or around an triggered event
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;- before an exception arises

### example

Imagine that you have the following 'Set' method class within the class Target in which you want to trace exceptions.
```
using System;
namespace Test
{
	public class Target :  MarshalByRefObject
	{
		int _P1;
		public int P1
		{
			get;
			set;
		}
		public void Set(object x1, object x2)
		{
			P1 = (int)x1/(int)x2;
		}
	}
}
```
With  **AspectDN**, you are able to solve your problem with the following aspect without changing your code.
```
using System;
using System.IO;
using System.Diagnostics;

package Aspects
{
	code => 
		extend around set properties : properties.Name == "P1" 
		with
		{
			var methodName = new StackFrame().GetMethod().ToString();
			using (StreamWriter sw = new StreamWriter(@"..\log.txt", true))
			{
				sw.WriteLine(string.Format("{0}: begin",methodName));
				try
				{
					[around anchor];
				}
				catch
				{
					sw.WriteLine(string.Format("{0}: catch",methodName));
				}
				finally
				{
					sw.WriteLine(string.Format("{0}: finally",methodName));
				}
				sw.WriteLine(string.Format("{0}: end",methodName));
			}
		}
}
```

Dont't hesitate to run the tutorial for more explanation on the use of AspectDN 

## Technologies

AspectDN needs several libraries are used and required to compile the project:
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;- Cecil of Jb Evain (version 0.11.0)
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;- Roslyn (CodeAnalysis.CSharp) (version 3.8.0)

AspectDN can only Microsoft .Net DLL and EXE file.

All tests have been done with Microsoft .NET Framework 4.6.1

## Installation
To install AspectDN and use it, you must copy the aspectdn.zip file from the repository containing the exe file and dll.  

## License
GNU GPLv3 with possible commercial licence including more functionalities.
Please contact us for more informations (aspectdn@gmail.com)

## Contributors
Contributos are wellcome.
All contributions are submitted to Contributor Assignment Agreemen (Individual or Corporate)
Please contact us to get the agreement or for more information about it (aspectdn@gmail.com)