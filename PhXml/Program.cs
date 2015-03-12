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

//			isiXml = "<?xml version=\"1.0\" ?>" +
//				"<data>" + 
//				"<type>reqinqpayment</type>" + 
//				"<vaid>500123000000123456</vaid>" + 
//				"<booking_datetime>2015-03-05 20:20:33</booking_datetime>" + 
//				"<reference_number>01</reference_number>" + 
//				"<username>partner</username>" + 
//				"<signature>ljshdfkuiy92716465286458264587</signature>" + 
//				"</data>";

			XmlSimple xml = new XmlSimple ();
			XmlSimple newxml = new XmlSimple ();

			xml.Clear (true);
			xml.XmlString = isiXml;
			newxml.Clear (false);
			newxml.XmlString = "<dummy/>";
			newxml.AddNode ("HUNTU", "isi");
		
			Console.WriteLine (xml.Name);
			Console.WriteLine (xml.ToString ());
			Console.WriteLine ();

			xml.Name = "Ruutt";
			xml.AddNode ("Satu", 12);
			xml.GetFirstChild ("Companies").AddNode ("Satu", "Kesemek");
			XmlSimple xmllain = xml.GetFirstChild ("Companies");
			xml.AddNode (newxml);
			xmllain.AddNode (newxml);
			Console.WriteLine (xml.ToString ());
			Console.WriteLine ();

			Console.WriteLine ("This Name = " + xml.Name);
			//List<XmlSimple> childs1 = xml.GetChildsList (xml.Name);
			XmlSimple[] childs1 = xml.GetChilds ();
			foreach (XmlSimple child in childs1) {
				Console.WriteLine (child.Name +" : "+ child.Value);
			}

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

