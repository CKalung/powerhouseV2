using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace PhXml
{
	public class XmlSimple : IDisposable
	{
		#region Disposable
		private bool disposed;
		public void Dispose()
		{
			if (!this.disposed)
			{
				this.Dispose(true);
				GC.SuppressFinalize(this);
			}
		}
		private void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					this.disposeAll();
				}
				this.disposed = true;
			}
		}
		~XmlSimple()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
			// disini dispose semua yang bisa di dispose
			if (this.isHasChild) {
				List<XmlSimple> childs = this.GetChildsList ();
				foreach (XmlSimple child in childs) {
					child.Dispose ();
				}
			}
			xmlElement = null;
		}

		//string xmlStr="";
		string xmlTemplate = "<?xml version=\"1.0\" ?>";
		XElement xmlElement=null;

		public XmlSimple ()
		{
			XmlString = "";
		}

		public XmlSimple (string xmlData)
		{
			XmlString = xmlData;
		}

		public XmlSimple (XmlSimple child)
		{
			GetSetXElement = child.GetSetXElement;
		}

		public string XmlString{
			get { 
				//return xmlStr;
				return xmlElement.ToString ();
			}
			set{
				//xmlStr = value;
				try{
					xmlElement = XElement.Parse (value);
				}catch{
					xmlElement = null;
				}
			}
		}

		public void Clear(bool isRoot = false){
			if(isRoot)
				XmlString = xmlTemplate;
			else
				XmlString = "";
		}

		public XElement GetSetXElement{
			get { return xmlElement; }
			set { 
				xmlElement = value; 
				//xmlStr = xmlElement.ToString (); 
			}
		}

		public string Name
		{
			get { return xmlElement.Name.ToString (); }
			set { 
				if (xmlElement == null)
					xmlElement = new XElement (value);
				else
					xmlElement.Name = value; }
		}

		public string Value
		{
			get { return xmlElement.Value; }
			set { xmlElement.Value = value; }
		}

		public void AddNode(string Name, object Value){
			//			xmlElement.Add (new XElement(Name, Value));
			xmlElement.SetElementValue (Name, Value);
		}

		public void UpdateNode(string Name, object Value){
			AddNode (Name, Value);
		}

		public void AddNode(XmlSimple Node){
			xmlElement.Add (Node.GetSetXElement);
		}

		public override string ToString ()
		{
			return (xmlTemplate +"\r\n"+ xmlElement.ToString ());
			//return string.Format ("[XmlSimpleData: XmlString={0}, Name={1}, Value={2}]", XmlString, Name, Value);
		}

		public bool isHasChild{
			get{ return (xmlElement.Elements ().Count () > 0); }
		}

		public string getChildValue(string Name){
			return xmlElement.Element (Name).Value;
		}

		public XmlSimple GetFirstChild(string Name){
			XElement child = xmlElement.Element (Name);
			if (child == null)
				return null;
			XmlSimple ret = new XmlSimple ();
			ret.GetSetXElement = child;
			return ret;
		}

		public List<XmlSimple> GetChildsList(string Name){
			List<XmlSimple> rets = new List<XmlSimple> ();
			foreach (XElement child in xmlElement.Elements(Name)) {
				if (child == null)
					return null;
				XmlSimple aRet = new XmlSimple ();
				aRet.GetSetXElement = child;
				rets.Add (aRet);
			}
			return rets;
		}

		public List<XmlSimple> GetChildsList(){
			List<XmlSimple> rets = new List<XmlSimple> ();
			foreach (XElement child in xmlElement.Elements()) {
				if (child == null)
					return null;
				XmlSimple aRet = new XmlSimple ();
				aRet.GetSetXElement = child;
				rets.Add (aRet);
			}
			return rets;
		}

		public XmlSimple[] GetChilds(string Name){
			int count = xmlElement.Elements (Name).Count ();
			XmlSimple[] rets = new XmlSimple[count];
			int i = 0;
			foreach (XElement child in xmlElement.Elements(Name)) {
				if (child == null)
					return null;
				XmlSimple aRet = new XmlSimple ();
				aRet.GetSetXElement = child;
				rets[i++] = aRet;
			}
			return rets;
		}

		public XmlSimple[] GetChilds(){
			int count = xmlElement.Elements().Count ();
			XmlSimple[] rets = new XmlSimple[count];
			int i = 0;
			foreach (XElement child in xmlElement.Elements()) {
				if (child == null)
					return null;
				XmlSimple aRet = new XmlSimple ();
				aRet.GetSetXElement = child;
				rets[i++] = aRet;
			}
			return rets;
		}

	}
}

