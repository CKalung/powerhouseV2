using System;
using System.Collections.Generic;

namespace PhXml
{
	class Program
	{
		static void Main(string[] args)
		{
//			SimpleXml xml = new SimpleXml ();
//			xml.test ();

			string isiXml = 
				"<?xml version=\"1.0\" ?>" +
				"<MyFile>  " + 
				"<Companies>  " + 
				"<Company>1230</Company>  " + 
				"<Company>4560</Company>  " + 
				"<Company>7890</Company>  " + 
				"</Companies> " + 
				"</MyFile>";

			XmlSimple xml = new XmlSimple ();

			xml.Clear (true);
			xml.XmlString = isiXml;
		
			Console.WriteLine (xml.ToString ());
			Console.WriteLine ();

			xml.Name = "Ruutt";
			xml.AddNode ("Satu", 12);
			xml.GetFirstChild ("Companies").AddNode ("Satu", "Kesemek");
			Console.WriteLine (xml.ToString ());
			Console.WriteLine ();

			Console.WriteLine ("isHasChild = " + xml.isHasChild);

			Console.WriteLine (xml.getChildValue ("Companies"));
			List<XmlSimple> childs = xml.GetFirstChild ("Companies").GetChildsList ("Company");
			foreach (XmlSimple child in childs) {
				Console.WriteLine (child.Name +" : "+ child.Value);
			}
			Console.WriteLine ();

			Console.WriteLine ("Enumerate by array");
			XmlSimple[] childsAr = xml.GetFirstChild ("Companies").GetChilds ("Company");
			for (int i = 0;i<childsAr.Length;i++){ 
				XmlSimple child = childsAr [i];
				Console.WriteLine (child.Name + "[" + i.ToString () + "] : "+ child.Value);
			}
			Console.WriteLine ();

			Console.WriteLine (xml.GetFirstChild ("Companies").getChildValue ("Company"));

			xml.Dispose ();
		}
	}
}

