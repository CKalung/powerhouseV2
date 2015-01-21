using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace JsonLibs
{
    public class MyJsonLib: Hashtable, IDisposable
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
        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
        }
        ~MyJsonLib()
        {
            this.Dispose(false);
        }
        #endregion

        

        public override void Add(object key, object value)
        {
            if (!key.GetType().Equals(typeof(System.String)))
            {
                string skey = key.ToString();
                base.Add(skey, value);
            }
            else
                base.Add(key, value);
        }
        public void Add(string key, object value)
        {
            base.Add(key, value);
        }

        /// <summary>
        /// baru support 1 layer data json dengan tipe string, int, boolean dan double
        /// </summary>
        /// <param name="jsonItems"></param>
        /// <returns></returns>
        public string JSONConstruct()
        {
            string json = "{";
            string fieldName = "";
            string sFieldValue = "";
            //object fieldValue = null;
            //Console.WriteLine("jumlah: " + jsonItems.Count.ToString());
            foreach (DictionaryEntry de in this)
            {
                if(json.Length>1) json+=",";
                fieldName = de.Key as string;
                if (de.Value == null)
                {
                    json += "\"" + fieldName + "\":null";
                }
                else if (de.Value.GetType().Equals(typeof(System.String)))
                {
                    sFieldValue = de.Value.ToString();
                    if ((sFieldValue[0] == '{') && (sFieldValue[sFieldValue.Length - 1] == '}'))
                    {
                        // sub json
                        json += "\"" + fieldName + "\":" + de.Value.ToString();
                    }
                    else
                    {
                        // string
                        json += "\"" + fieldName + "\":\"" + de.Value.ToString() + "\"";
                    }

                }
                else if ((de.Value.GetType().Equals(typeof(System.Int32))) ||
                    (de.Value.GetType().Equals(typeof(System.Boolean))) ||
                    (de.Value.GetType().Equals(typeof(System.Double))))
                {
                    json += "\"" + fieldName + "\":" + de.Value.ToString();
                }
                //Console.WriteLine("Type: " + de.Value.GetType().ToString());
                //Console.WriteLine(fieldName);
                //fieldValue = de.Value;
                //Console.WriteLine(fieldValue.ToString());
            }

            json += "}";

            //var fieldNames = typeof(MyJsonLib).GetFields()
            //                .Select(field => field.Name)
            //                .ToList();
            return json;
        }

        bool addJsonItem(string fieldName, string tmpFieldValue)
        {
            int intFieldValue = 0;
            double dblFieldValue = 0;
            string nValUpper = tmpFieldValue.ToUpper();

            fieldName = fieldName.Trim();
            if (fieldName.Length == 0) return false;
            if ((fieldName[0] != '"') || (fieldName[fieldName.Length - 1] != '"')) return false;
            fieldName = fieldName.Substring(1, fieldName.Length - 2);
            tmpFieldValue = tmpFieldValue.Trim();
            // cek string
            if ((tmpFieldValue[0] == '"') && (tmpFieldValue[tmpFieldValue.Length - 1] == '"'))
            {
                // string
                this.Add(fieldName, tmpFieldValue.Substring(1, tmpFieldValue.Length - 2));
                return true;
            }
            // cek sub Json
            if ((tmpFieldValue[0] == '[') && (tmpFieldValue[tmpFieldValue.Length - 1] == ']'))
            {
                // subJson
                this.Add(fieldName, tmpFieldValue);
                return true;
            }
            // cek NULL
            else if (nValUpper == "NULL")
            {
                this.Add(fieldName,null);
                return true;
            }
            // cek boolean
            else if ((nValUpper == "TRUE") || (nValUpper == "FALSE"))
            {
                this.Add(fieldName, (nValUpper[0] == 'T'));
                return true;
            }
            // cek double
            else if (tmpFieldValue.Contains('.'))
            {
                try
                {
                    dblFieldValue = double.Parse(tmpFieldValue);
                }
                catch { return false; }
                this.Add(fieldName, dblFieldValue);
                return true;
            }
            // cek int
            else
            {
                try
                {
                    intFieldValue = int.Parse(tmpFieldValue);
                }
                catch { return false; }
                this.Add(fieldName, intFieldValue);
                return true;
            }
        }

        /// <summary>
        /// baru support 1 layer data json dengan tipe string, int, boolean dan double
        /// </summary>
        /// <returns></returns>
        public bool JSONParse(string jsonData)
        {
            //if (jsonTable == null) jsonTable = new Hashtable();
            if (this.Count > 0) this.Clear();
            if (jsonData[0] != '{') return false;
            bool seekName = true;
            bool seekValue = false;
            bool completed = false;
            string fieldName = "";
            string tmpFieldValue = "";
            char firstCharOfValue = '\0';
            bool fAmbilStringValue = false;

            try
            {
                for (int i = 1; i < jsonData.Length; i++)
                {
                    if ((jsonData[i] == '\r') || (jsonData[i] == '\n')) continue;
                    if (fAmbilStringValue) // jika value tipe string
                    {
                        //tampung value                        
                        if (jsonData[i] != '"')
                        {
                            tmpFieldValue += jsonData[i];
                            continue;
                        }
                        else
                        {
                            // akhir data string
                            //firstCharOfValue = '\0';
                            tmpFieldValue += jsonData[i];
                            //seekValue = false;
                            fAmbilStringValue = false;
                            continue;
                        }
                    }

                    if (seekValue && (firstCharOfValue == '\0'))
                    {
                        if (jsonData[i] == ' ') continue;
                        firstCharOfValue = jsonData[i];
                        if (firstCharOfValue == '"')
                        {
                            fAmbilStringValue = true;
                            tmpFieldValue += jsonData[i];
                            continue;
                        }
                    }

                    if (seekName && (jsonData[i] == ':'))
                    {
                        // mulai masuk ke wilayah data
                        seekName = false;
                        seekValue = true;
                        firstCharOfValue = '\0';
                        continue;
                    }
                    else if (seekValue && (jsonData[i] == '}'))
                    {
                        // beres sepasang
                        if (addJsonItem(fieldName, tmpFieldValue))
                        {
                            // completed
                            completed = true;
                        }
                        break;
                    }
                    else if (seekValue && (jsonData[i] == ','))
                    {
                        seekName = true;
                        seekValue = false;
                        firstCharOfValue = '\0';
                        // beres sepasang
                        if (addJsonItem(fieldName, tmpFieldValue))
                        {
                            fieldName = "";
                            tmpFieldValue = "";
                            continue;
                        }
                        else break;
                    }
                    else
                    {
                        if (seekName)
                        {
                            // tampung calon nama
                            fieldName += jsonData[i];
                            continue;
                        }
                        else if (seekValue)
                        {
                            //tampung value
                            tmpFieldValue += jsonData[i];
                            continue;
                        }
                        //else seekName = true;
                    }
                }
            }
            catch
            {
                this.Clear();
                return false;
            }
            if (completed) return true;
            else
            {
                this.Clear();
                return false;
            }
        }

    }
}
