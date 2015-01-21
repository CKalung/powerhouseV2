using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace JSONTester
{
    class showPair
    {
        public void viewObject(string parentName, JsonLibs.MyJsonLib jsonlib)
        {
            Console.WriteLine("");
            foreach (DictionaryEntry dic in jsonlib)
            {
                //Console.WriteLine("KEY : " + dic.Key + ", values: " + dic.Value.GetType().ToString());
                if (dic.Value == null)
                {
                    Console.WriteLine(parentName + "->" + "\"" + (string)dic.Key + "\" = null");
                    //Console.WriteLine("Tipe : NULL");
                    continue;
                }
                else if (dic.Value.GetType().Equals(typeof(System.String)))
                    Console.WriteLine(parentName + "->" + "\"" + (string)dic.Key + "\" = \"" + (System.String)dic.Value + "\"");
                else if (dic.Value.GetType() == typeof(JsonLibs.MyJsonLib))
                {
                    Console.WriteLine("");
                    Console.WriteLine("**************");
                    Console.WriteLine(parentName + "->" + "\"" + (string)(dic.Key) + "\" = " + dic.Value.ToString());
                    Console.WriteLine("--------------");
                    showPair shw = new showPair();
                    shw.viewObject(parentName + "->" + (string)(dic.Key),
                        (JsonLibs.MyJsonLib)dic.Value);
                }
                else if (dic.Value.GetType() == typeof(JsonLibs.MyJsonArray))
                {
                    Console.WriteLine("===============");
                    Console.WriteLine(parentName + "->" + "\"" + (string)(dic.Key) + "\" = " + dic.Value.ToString());
                    Console.WriteLine("===============");
                    showPair shw = new showPair();
                    shw.viewArray(parentName + "->" + (string)(dic.Key),
                        (JsonLibs.MyJsonArray)dic.Value);
                }
                else
                    Console.WriteLine(parentName + "->" + "\"" + dic.Key.ToString() + "\" = " + dic.Value.ToString());
                //Console.WriteLine("Tipe : " + dic.Value.GetType().ToString());
            }
        }
        public void viewArray(string parentName, JsonLibs.MyJsonArray jsonArray)
        {
            object dic;
            for(int i = 0;i<jsonArray.Count;i++)
            //foreach (object dic in jsonArray)
            {
                Console.WriteLine("");
                dic = jsonArray[i];
                //Console.WriteLine("KEY : " + dic.Key + ", values: " + dic.Value.GetType().ToString());
                if (dic == null)
                {
                    Console.WriteLine(parentName + "[" + i.ToString() + "] = null");
                    //Console.WriteLine("Tipe : NULL");
                    continue;
                }
                else if (dic.GetType().Equals(typeof(System.String)))
                    Console.WriteLine(parentName + "[" + i.ToString() + "] = \"" + (System.String)dic + "\"");
                else if (dic.GetType() == typeof(JsonLibs.MyJsonLib))
                {
                    Console.WriteLine("");
                    Console.WriteLine("**************");
                    //Console.WriteLine(parentName + "->" + " = " + dic.ToString());
                    Console.WriteLine(parentName + "[" +i.ToString()+ "] = " + dic.ToString());
                    Console.WriteLine("--------------");
                    showPair shw = new showPair();
                    shw.viewObject(parentName +"[" +i.ToString()+ "]",
                        (JsonLibs.MyJsonLib)dic);
                }
                else if (dic.GetType() == typeof(JsonLibs.MyJsonArray))
                {
                    Console.WriteLine("===============");
                    Console.WriteLine(parentName + "[" + i.ToString() + "] = " + dic.ToString());
                    Console.WriteLine("===============");
                    showPair shw = new showPair();
                    shw.viewArray(parentName + "[" + i.ToString() + "]",
                        (JsonLibs.MyJsonArray)dic);
                }
                else
                    Console.WriteLine(parentName + "[" + i.ToString() + "] = " + dic.ToString());
                //Console.WriteLine("Tipe : " + dic.Value.GetType().ToString());
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string strJson="";

            using (JsonLibs.MyJsonLib jsonH = new JsonLibs.MyJsonLib())
            {
                jsonH.Add("data1", 123);
                jsonH.Add("data2", "huntu");
                jsonH.Add("data3", true);
                jsonH.Add("data4", 1.23);
                jsonH.Add("data5", null);
                Console.WriteLine("jumlah: " + jsonH.Count.ToString());

                strJson = jsonH.JSONConstruct();
                Console.WriteLine(strJson);
            }

            using (JsonLibs.MyJsonLib tesJson = new JsonLibs.MyJsonLib())
            {
                if (tesJson.JSONParse(strJson))
                {
                    Console.WriteLine("Parsing berhasil");
                    Console.WriteLine("======================");
                    foreach (DictionaryEntry dic in tesJson)
                    {
                        if (dic.Value == null)
                        {
                            Console.WriteLine("\"" + dic.Key.ToString() + "\" = null");
                            Console.WriteLine("Tipe : NULL");
                            continue;
                        }
                        else if (dic.Value.GetType().Equals(typeof(System.String)))
                            Console.WriteLine("\"" + dic.Key.ToString() + "\" = \"" + (System.String)dic.Value + "\"");
                        else
                            Console.WriteLine("\"" + dic.Key.ToString() + "\" = " + dic.Value.ToString());
                        Console.WriteLine("Tipe : " + dic.Value.GetType().ToString());
                    }
                }
                else
                {
                    // gagal parse
                    Console.WriteLine("Gagal parsing");
                }
            }

            //string jsonData = System.IO.File.ReadAllText("ContohJson.txt");
            //string jsonData = System.IO.File.ReadAllText("ContohJson2.txt");
            string jsonData = System.IO.File.ReadAllText("ContohJson3.txt");

            using (JsonLibs.MyJsonLib tesJson = new JsonLibs.MyJsonLib())
            {
//                if (tesJson.JSONParse(strJson))
                if (tesJson.JSONParse(jsonData))
                {
                    Console.WriteLine("Parsing berhasil");
                    Console.WriteLine("======================");
                    showPair shw = new showPair();
                    shw.viewObject("",tesJson);
                    //foreach (DictionaryEntry dic in tesJson.result)
                    //{
                    //    if (dic.Value == null)
                    //    {
                    //        Console.WriteLine("\"" + dic.Key.ToString() + "\" = null");
                    //        Console.WriteLine("Tipe : NULL");
                    //        continue;
                    //    }
                    //    else if (dic.Value.GetType().Equals(typeof(System.String)))
                    //        Console.WriteLine("\"" + dic.Key.ToString() + "\" = \"" + (System.String)dic.Value + "\"");
                    //    else if (dic.Value.GetType() == typeof(JsonLibs.MyJsonLib2))
                    //    {
                    //        Console.WriteLine("Tipe : " + dic.Value.GetType().ToString());

                    //    }
                    //    else
                    //        Console.WriteLine("\"" + dic.Key.ToString() + "\" = " + dic.Value.ToString());
                    //    Console.WriteLine("Tipe : " + dic.Value.GetType().ToString());
                    //}

                    string appID = "";
                    string userPhone = "";
                    string customerProductNumber = "";
                    string productCode = "";
                    int transactionType = 0;
                    string tipe = tesJson["fiTransactionType"].GetType().ToString();
                    try
                    {
                        appID = ((string)tesJson["fiApplicationId"]).Trim();
                        userPhone = ((string)tesJson["fiPhone"]).Trim();
                        customerProductNumber = ((string)tesJson["fiCustomerNumber"]).Trim();
                        productCode = ((string)tesJson["fiProductCode"]).Trim();
                        transactionType = (int)tesJson["fiTransactionType"];
                    }
                    catch
                    {
                        Console.WriteLine("Invalid field type");
                    }

                    Console.WriteLine();
                    Console.WriteLine( tesJson.JSONConstruct());
                }
                else
                {
                    // gagal parse
                    Console.WriteLine("Gagal parsing");
                }
            }

            Console.ReadLine();
        }
    }
}
