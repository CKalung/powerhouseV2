using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JsonLibs
{
    public class MyJsonArray : List<object>, IDisposable
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
        ~MyJsonArray()
        {
            this.Dispose(false);
        }
        #endregion

        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
            // dispose semua array yang terkandung didalam root json dan semua subjson nya
            foreach (object de in this)
            {
                if (de == null) continue;
                if (de.GetType().Equals(typeof(MyJsonArray)))
                {
                    try
                    {
                        ((MyJsonArray)de).Dispose();
                    }
                    catch { }
                }
                if (de.GetType().Equals(typeof(MyJsonLib)))
                {
                    try
                    {
                        ((MyJsonLib)de).Dispose();
                    }
                    catch { }
                }
            }
        }

        public string Name = "";
        public MyJsonLib Parent = null;
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonArray"/> class. 
        /// </summary>
        public MyJsonArray() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonArray"/> class. 
        /// </summary>
        /// <param name="capacity">The capacity of the json array.</param>
        public MyJsonArray(int capacity) : base(capacity) { }

        ///// <summary>
        ///// The json representation of the array.
        ///// </summary>
        ///// <returns>The json representation of the array.</returns>
        //public override string ToString()
        //{
        //    return SerializeObject(this) ?? string.Empty;
        //}

        public string Construct()
        {
            {
                string json = "[";
                string sFieldValue = "";
                bool awal = true;
                //object fieldValue = null;
                //Console.WriteLine("jumlah: " + jsonItems.Count.ToString());
                //foreach (object de in this)
                foreach (object de in this)
                {
                    if (!awal) { json += ","; };
                    awal = false; 
                    //if (json.Length > 1) json += ",";
                    if (de == null)
                    {
                        json += "null";
                    }
                    else if (de.GetType().Equals(typeof(System.String)))
                    {
                        sFieldValue = de.ToString();
                        if (sFieldValue.Length == 0)
                        {
                            // string
                            json += "\"\"";
                        }
                        else if ((sFieldValue[0] == '{') && (sFieldValue[sFieldValue.Length - 1] == '}'))
                        {
                            // sub json
                            json += de.ToString();
                        }
                        else if ((sFieldValue[0] == '[') && (sFieldValue[sFieldValue.Length - 1] == ']'))
                        {
                            // array
                            json += de.ToString();
                        }
                        else
                        {
                            // string
                            json += "\"" + de.ToString() + "\"";
                        }
                    }
                    else if ((de.GetType().Equals(typeof(System.Int32))) ||
                        (de.GetType().Equals(typeof(System.Int64))) ||
                        (de.GetType().Equals(typeof(System.Boolean))) ||
                        (de.GetType().Equals(typeof(System.Double))))
                    {
                        json += de.ToString();
                    }
                    else if (de.GetType().Equals(typeof(JsonLibs.MyJsonLib)))
                    {
                        json += ((MyJsonLib)de).JSONConstruct();
                    }
                    else if (de.GetType().Equals(typeof(JsonLibs.MyJsonArray)))
                    {
                        json += ((MyJsonArray)de).Construct();
                    }
                    //Console.WriteLine("Type: " + de.Value.GetType().ToString());
                    //Console.WriteLine(fieldName);
                    //fieldValue = de.Value;
                    //Console.WriteLine(fieldValue.ToString());
                }

                json += "]";

                //var fieldNames = typeof(MyJsonLib).GetFields()
                //                .Select(field => field.Name)
                //                .ToList();
                return json;
            }
        }
    }
}
