using System;
using System.Text;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Collections;
using System.Collections.Generic;

namespace PhXml
{
	public class MyXmlNode
	{
		public MyXmlNode ()
		{

		}

		public void test(){
			string isiXml = 
				"<?xml version=\"1.0\" ?>" +
				"<MyFile>  " + 
				"<Companies>  " + 
				"<Company>1230</Company>  " + 
				"<Company>4560</Company>  " + 
				"<Company>7890</Company>  " + 
				"</Companies> " + 
				"</MyFile>";
//			XmlDocument xml = new XmlDocument();
//			xml.LoadXml (isiXml);
			XDocument xml = XDocument.Parse (isiXml);
			XmlReader reader = xml.CreateReader ();
			XmlWriter writer = xml.CreateWriter ();

			var companyIds = xml.Descendants("Company").Select(e => (int)e);


			XElement xel = XElement.Parse (isiXml);

			//List<XAttribute> xar = xel.Attributes ();
			Console.WriteLine ("asup " + xel.Elements ().Count ());

			//xel.Descendants (XName.Get ("MyFile")).Elements("Company");
			Console.WriteLine ("Name root " + xel.Name);
			foreach (XElement xel2 in xel.Elements ()) {
				Console.WriteLine ("Name l1 " + xel2.Name);
				Console.WriteLine ("asup1 " + xel2.Elements ().Count ());
				foreach (XElement xel1 in xel2.Elements ()) {
					Console.WriteLine ("asup2 " + xel1.Elements ().Count ());
					if (xel2.Value != null) {
						Console.WriteLine ("Name l2 " + xel1.Name);
						Console.WriteLine ("Descendants " + xel1.Value);
					}
				}
			}

			//xel.Descendants (XName.Get ("MyFile")).Elements("Company");
			Console.WriteLine ("Name root " + xel.Name);
			foreach (XElement xel2 in xel.Elements ()) {
				Console.WriteLine ("Name l1 " + xel2.Name);
				Console.WriteLine ("asup1 " + xel2.Elements ().Count ());
				foreach (XElement xel1 in xel2.Elements ()) {
					Console.WriteLine ("asup2 " + xel1.Elements ().Count ());
					if (xel2.Value != null) {
						Console.WriteLine ("Name l2 " + xel1.Name);
						xel1.SetValue ("abcd");
						Console.WriteLine ("Descendants " + xel1.Value);
					}
				}
			}

			//xel.Descendants (XName.Get ("MyFile")).Elements("Company");
			foreach (XElement xel2 in xel.Elements ()) {
				Console.WriteLine ("asup1 " + xel2.Elements ().Count ());
				foreach (XElement xel1 in xel2.Elements ()) {
					Console.WriteLine ("asup2 " + xel1.Elements ().Count ());
					if (xel2.Value != null) {
						Console.WriteLine ("Name " + xel1.Name);
						Console.WriteLine ("Descendants " + xel1.Value);
					}
				}
			}

			//XElement xel1 = xel.Elements()

			foreach (XAttribute att in xel.Attributes ()) {
				Console.WriteLine (att.Value);
			}

			if (xel.Attribute("MyFile") != null)
			{
				Console.WriteLine ( xel.Attribute("MyFile").Value);
			}

			Console.WriteLine (GetOutline(0,xel));


//			List<String> data = xml.Descendants("Companies").Elements("Company")
//				Select(r => r.Value).ToArray();


			//XmlReader reader = XmlReader.Create ();

//			Contoh Xml dan cara bacanya
//			<MyFile> 
//			<Companies> 
//			<Company>123</Company> 
//			<Company>456</Company>
//			<Company>789</Company> 
//			</Companies> 
//			</MyFile>
//
//			var test = xmlDoc.Descendants("Companies").Elements("Company").Select(r => r.Value).ToArray();
//			string result = string.Join(",", test);


		}

		private string GetOutline(int indentLevel, XElement element)
		{
			StringBuilder result = new StringBuilder();

			if (element.Attribute("Company") != null)
			{
				result = result.AppendLine(new string(' ', indentLevel * 2) + element.Attribute("name").Value);
			}

			foreach (XElement childElement in element.Elements())
			{
				result.Append(GetOutline(indentLevel + 1, childElement));
			}

			return result.ToString();
		}
	}
}

