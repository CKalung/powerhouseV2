using System;
using PPOBHttpRestData;
using System.Collections;
using System.Collections.Generic;
using PPOBDatabase;

namespace Process_ProductTransaction
{
	public class ProductQuery : IDisposable {
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
		~ProductQuery()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
			// disini dispose semua yang bisa di dispose
		}

		/******************************************************************************/
		HTTPRestConstructor.HttpRestRequest clientData;
		PublicSettings.Settings commonSettings;
		JsonLibs.MyJsonLib jsonConv;
		HTTPRestConstructor HTTPRestDataConstruct;
		PPOBDatabase.PPOBdbLibs localDB;
		Exception xError;

		// ke ieu asukeun dina config di database
		string kodeQuery1_Pembelian = "PHQ0";
		string kodeQuery1_Pembayaran = "PHQ1";
		string kodeQuery2_Pulsa="002";		// karena khusus, pulsa jadi di definiskan didieu

		/******************************************************************************/

		public ProductQuery (HTTPRestConstructor.HttpRestRequest ClientData,
			PublicSettings.Settings CommonSettings)
		{
			clientData = ClientData;
			commonSettings = CommonSettings;
			HTTPRestDataConstruct = new HTTPRestConstructor();
			jsonConv = new JsonLibs.MyJsonLib();
			localDB = new PPOBDatabase.PPOBdbLibs(commonSettings.DbHost, commonSettings.DbPort,
				commonSettings.DbName, commonSettings.DbUser, commonSettings.DbPassw);
		}

		#region Kumpulan fungsi standar

		/*============================================================================*/
		/*   Kumpulan fungsi standar */
		/*============================================================================*/

		private void ReformatPhoneNumber(ref string phone)
		{
			phone = phone.Trim ().Replace(" ","").Replace("'","").Replace("-","");
			if (phone.Length < 2)
			{
				phone = "";
				return;
			}
			if (phone[0] == '0')
			{
				if (phone[1] == '6')
					phone = phone.Substring(1);
				else
					phone = "62" + phone.Substring(1);
			}
			else if (phone[0] == '+') phone = phone.Substring(1);
		}

		private bool checkMandatoryFields(string[] mandatoryFields)
		{
			foreach (string aField in mandatoryFields)
			{
				if (!jsonConv.ContainsKey(aField))
				{
					return false;
				}
			}
			return true;
		}

		/*============================================================================*/
		/*============================================================================*/

		#endregion



		public bool queryPembelianPembayaran(string appID, bool isPembelian, ref string httpReply,
			ref List<PPOBdbLibs.ProductDetailsStruct> hasil){
			Hashtable pgList = null;
			if (!localDB.getProductGroupList (appID, isPembelian, ref pgList, out xError)) {
				httpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Query ProductGroup failed!", "");
				return false;
			}
			foreach (DictionaryEntry entri in pgList) {
				PPOBdbLibs.ProductDetailsStruct anItemDetails = new PPOBdbLibs.ProductDetailsStruct ();
				//anItemDetails.list_code = 0;	// group produk
				if (isPembelian)
					anItemDetails.code = "PHQ0";
				else
					anItemDetails.code = "PHQ1";
				anItemDetails.code += entri.Key.ToString ().PadLeft (3,'0');
				anItemDetails.name = (string)entri.Value;
				hasil.Add (anItemDetails);
			}
			return true;
		}

		private string getNextQueryCode(ref string queryCode){
			string hasil = "";
			try{
				hasil = queryCode.Substring (0,3);
			}
			catch{
				return "";
			}
			try{
				queryCode = queryCode.Substring(3);
			}catch{
				queryCode = "";
			}
			return hasil;
		}

		private bool queryLanjutanGroupProduct (string appID, string queryCode, 
			string outlet_code, string keyword, 		// kosongkan jika gak ada
			int rangeFrom, int rangeLength, 
			ref string httpReply, ref int listCode,
			ref List<PPOBdbLibs.ProductDetailsStruct> hasil, ref Hashtable hasilNitrogenProduct){

			bool isPembelian = (queryCode [3] == '0');
			string strQCode = queryCode.Substring (4);
			string QCode = getNextQueryCode(ref strQCode);
			Hashtable hasilTbl=null;
			if (isPembelian) {
				// pembelian
				// query dulu untuk mendapatkan query berikutnya
				if (QCode == commonSettings.getString ("ProductGroupCode-PULSA")) {
					if(!localDB.getPulsaPrefixList(appID, QCode,ref hasilTbl,out xError)){
						// Jika gagal query
						httpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Query prefix pulsa failed!", "");
						return false;
					}
					if (strQCode.Length == 0) {
						// sampe query prefix saja
						foreach (DictionaryEntry entri in hasilTbl) {
							PPOBdbLibs.ProductDetailsStruct anItemDetails = new PPOBdbLibs.ProductDetailsStruct ();
							//anItemDetails.list_code = 0;	// group produk
							anItemDetails.code = queryCode;	// sambung dari query code sebelumnya
							anItemDetails.code += entri.Key.ToString ().PadLeft (3,'0');
							anItemDetails.name = (string)entri.Value;
							hasil.Add (anItemDetails);
						}
						listCode = 0;	// masih grup
						return true;
					} else {
						// setelah prefix masih ada lagi yang musti di query
						// ambil prefixnya dulu untuk keperluan query
						QCode = getNextQueryCode(ref strQCode);	// ambil kodequery setelah prefix
						int iQCode = 0;
						try{
							iQCode = int.Parse (QCode);
						}catch{
							httpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Invalid Prefix code of Pulsa!", "");
							return false;
						}
						if (!hasilTbl.ContainsKey (iQCode)) {
							// jika tidak terdaftar index prefixnya
							httpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Invalid Prefix code of Pulsa!", "");
							return false;
						}
						string prefixPulsa = ((string)(hasilTbl [iQCode])).Trim();
						// lanjut ke query produk pulsa dengan prefix 
						listCode = 1;	// udah produk
						if(!localDB.getPulsaProductList(appID, prefixPulsa, rangeFrom, rangeLength, ref hasil, 
							out xError)){
							// Jika gagal query
							httpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Query produk pulsa failed!", "");
							return false;
						}
						return true;
					}
				} else if (QCode == commonSettings.getString ("ProductGroupCode-SHOPS")) {
					// masuk ke query TOko
					// check jika lanjutannya (query) untuk toko-toko
					// Query Kategori Produk
					if (!localDB.getShopsProductCategory (appID, ref hasilTbl, out xError)){
						httpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Query Shops Product Category failed!", "");
						return false;
					}
					if (strQCode.Length == 0) {
						// sampe query kategori produk saja
						foreach (DictionaryEntry entri in hasilTbl) {
							PPOBdbLibs.ProductDetailsStruct anItemDetails = new PPOBdbLibs.ProductDetailsStruct ();
							//anItemDetails.list_code = 0;	// group produk
							anItemDetails.code = queryCode;	// sambung dari query code sebelumnya
							anItemDetails.code += entri.Key.ToString ().PadLeft (3, '0');
							anItemDetails.name = (string)entri.Value;
							hasil.Add (anItemDetails);
						}
						listCode = 2;	// masih grup, khusus kategori produk toko
						return true;
					} else {
						// Setelah kategori, query shops product list
						QCode = getNextQueryCode(ref strQCode);	
						int iQCode = 0;
						try{
							iQCode = int.Parse (QCode);
						}catch{
							httpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Invalid Category code of shops!", "");
							return false;
						}
						if (!hasilTbl.ContainsKey (iQCode)) {
							// jika tidak terdaftar kategorinya
							httpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Invalid Category code of shops!", "");
							return false;
						}
						string KategoriProdukToko = ((string)(hasilTbl [iQCode])).Trim();
						// lanjut ke query produk toko dengan kategori dan preference search 
						listCode = 1;	// udah produk
						if(!localDB.getShopsProductDetails(appID, outlet_code, keyword, iQCode,
							rangeFrom, rangeLength, ref hasil, out xError)){
							// Jika gagal query
							httpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Query shops products failed!", "");
							return false;
						}
						return true;
					}
//				} else if (QCode == commonSettings.getString ("ProductGroupCode-NITROGEN")) {
//					// Query group prefix nitrogen
//					if(!localDB.getNitrogenPrefixList(QCode,ref hasilTbl,out xError)){
//						// Jika gagal query
//						httpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Query prefix nitrogen failed!", "");
//						return false;
//					}
//					if (strQCode.Length == 0) {
//						// sampe query prefix saja
//						foreach (DictionaryEntry entri in hasilTbl) {
//							PPOBdbLibs.ProductDetailsStruct anItemDetails = new PPOBdbLibs.ProductDetailsStruct ();
//							//anItemDetails.list_code = 0;	// group produk
//							anItemDetails.code = queryCode;	// sambung dari query code sebelumnya
//							anItemDetails.code += entri.Key.ToString ().PadLeft (3,'0');
//							anItemDetails.name = (string)entri.Value;
//							hasil.Add (anItemDetails);
//						}
//						listCode = 0;	// masih grup
//						return true;
//					} else {
//						// setelah prefix nitrogen, masih ada lagi yang musti di query
//						// ambil prefixnya dulu untuk keperluan query produknya
//						QCode = getNextQueryCode(ref strQCode);	// ambil kodequery setelah prefix
//						int iQCode = 0;
//						try{
//							iQCode = int.Parse (QCode);
//						}catch{
//							httpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Invalid Prefix code of Nitrogen!", "");
//							return false;
//						}
//						if (!hasilTbl.ContainsKey (iQCode)) {
//							// jika tidak terdaftar index prefixnya
//							httpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Invalid Prefix code of Nitrogen!", "");
//							return false;
//						}
//						string prefixNitrogen = ((string)(hasilTbl [iQCode])).Trim();
//						// lanjut ke query produk nitrogen dengan prefix 
//						listCode = 1;	// udah produk
//						if(!localDB.getNitrogenProductList(prefixNitrogen, rangeFrom, rangeLength, ref hasil, 
//							out xError)){
//							// Jika gagal query
//							httpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Query produk nitrogen failed!", "");
//							return false;
//						}
//						return true;
//					}
				} else if (QCode == commonSettings.getString ("ProductGroupCode-NITROGEN")) {
					// Query group prefix nitrogen
					if(!localDB.getNitrogenGroupProductList(QCode, rangeFrom, rangeLength, ref hasilTbl,out xError)){
						// Jika gagal query
						httpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Query nitrogen products failed!", "");
						return false;
					}

					hasilNitrogenProduct = hasilTbl;
					// bikin json dari hashtable
//					foreach (DictionaryEntry entri in hasilTbl) {
//						List<PPOBdbLibs.ProductDetailsStruct> aPrefixList = (List<PPOBdbLibs.ProductDetailsStruct>)entri.Value;
					listCode = 3;	// khusus list produk nitrogen yang ada subjson
					return true;

				} else {
					// pembelian selain pulsa
					int iQCode = 0;
					try{
						iQCode = int.Parse (QCode);
					}catch{
						httpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Invalid purchase query code!", "");
						return false;
					}
					// lanjut ke query produk selain pulsa
					listCode = 1;	// udah produk
					if(!localDB.getPurchaseProductList(appID, iQCode, rangeFrom, rangeLength, ref hasil, 
						out xError)){
						// Jika gagal query
						httpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Query product failed!", "");
						return false;
					}
					return true;
				}
			} else {
				// pembayaran
				int iQCode = 0;
				try{
					iQCode = int.Parse (QCode);
				}catch{
					httpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Invalid payment query code!", "");
					return false;
				}
				listCode = 3;	// udah produk pembayaran, tingga inquiry
				if(!localDB.getPaymentProductList(appID, iQCode, rangeFrom, rangeLength, ref hasil, 
					out xError)){
					// Jika gagal query
					httpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Query product failed!", "");
					return false;
				}
				return true;
			}
			return false;
		}

		public bool queryOutletList(string appID, 
			int rangeFrom, int rangeLength, 
			ref string httpReply, ref int listCode,
			ref List<PPOBdbLibs.OutletListStruct> hasil){

			listCode = 4;

			Hashtable pgList = null;
			if (!localDB.getOutletList(appID, rangeFrom, rangeLength, ref hasil, out xError)){
				httpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Query Outlet list failed!", "");
				return false;
			}
			httpReply = "";
			return true;
		}

		public string ProductListInquiry(){
			string[] fields = { "fiApplicationId", "fiProductQueryCode",  "fiRange"};

			string appID = "";
			string queryCode = "";
			string range = "";

			string shops_outlet_code=""; 
			string shops_keyword="";


			if (!jsonConv.JSONParse(clientData.Body))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
			}

			if(!checkMandatoryFields(fields))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory fields not found", "");
			}

			try
			{
				appID = ((string)jsonConv["fiApplicationId"]).Trim();
				queryCode = ((string)jsonConv["fiProductQueryCode"]).Trim ();
				range = ((string)jsonConv["fiRange"]).Trim ();
			}
			catch
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
			}

			if (jsonConv.isExists ("fiKeyword")) {
				shops_keyword = ((string)jsonConv ["fiKeyword"]).Trim ();
			}
			if (jsonConv.isExists ("fiOutletId")) {
				shops_outlet_code = ((string)jsonConv ["fiOutletId"]).Trim ();
			}

			string[] aRange = range.Split (new char[] {'-'}, StringSplitOptions.RemoveEmptyEntries);

			if (aRange.Length != 2) {
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid range field type or format", "");
			}

			int rangeFrom = 0;
			int rangeTo = 0;

			try{
				rangeFrom = int.Parse (aRange[0]);
				rangeTo = int.Parse (aRange[1]);
			}catch{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid range field type or format", "");
			}

			string httpReply = "";
			List<PPOBdbLibs.ProductDetailsStruct> hasil = new List<PPOBdbLibs.ProductDetailsStruct> ();
			int list_code=0;	// 0 = grouplist; 1 = productlist
			Hashtable hasilTbl = null;
			List<PPOBdbLibs.OutletListStruct> hasilOutlet = null;

			// Switch ke masing-masing cara query berdasarkan queryCode disini
			if ((queryCode == "PHQ0") || (queryCode == "PHQ1")) {
				// Query Pembelian/pembayaran
				list_code = 0;
				if (!queryPembelianPembayaran (appID, queryCode == "PHQ0", ref httpReply, ref hasil)) {
					return httpReply;
				}
			} else if (queryCode == "PHQ2") {
				// Query Outlet list
				list_code = 4;
				if (!queryOutletList (appID, 
					rangeFrom, rangeTo, 
					ref httpReply, ref list_code, ref hasilOutlet)) {
					return httpReply;
				}
			} else if(queryCode.StartsWith ("PHQ") && (queryCode.Length>6)){
				// jika query
				if (!queryLanjutanGroupProduct (appID, queryCode, shops_outlet_code, shops_keyword, 
					rangeFrom, rangeTo, 
					ref httpReply, ref list_code, ref hasil, ref hasilTbl)) {
					return httpReply;
				}
			} else {
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "494", "Invalid product query code", "");
			}

			JsonLibs.MyJsonArray arPg = new JsonLibs.MyJsonArray ();

			if (hasilOutlet != null) {
				foreach (PPOBdbLibs.OutletListStruct outlet in hasilOutlet) {
					JsonLibs.MyJsonLib aPrefix = new JsonLibs.MyJsonLib ();

					aPrefix.Add ("fiOutletCode", outlet.code);
					aPrefix.Add ("fiOUtletName", outlet.name);
					aPrefix.Add ("fiOUtletPhone", outlet.phone);

					arPg.Add (aPrefix);
				}
			} else if (hasilTbl != null) {
				// array sub json nitrogen
				List<PPOBdbLibs.ProductDetailsStruct> aPrefixList;
				foreach (DictionaryEntry entri in hasilTbl) {
					aPrefixList = (List<PPOBdbLibs.ProductDetailsStruct>)entri.Value;
					JsonLibs.MyJsonArray arPrfxPg = new JsonLibs.MyJsonArray ();
					for (int i = 0; i < aPrefixList.Count; i++) {
						JsonLibs.MyJsonLib aPGroup = new JsonLibs.MyJsonLib ();
						aPGroup.Add ("fiItemCode", aPrefixList [i].code);
						aPGroup.Add ("fiItemName", aPrefixList [i].name);
						aPGroup.Add ("fiPrice", aPrefixList [i].price);
						aPrefixList [i].Dispose ();

						arPrfxPg.Add (aPGroup);
					}
					JsonLibs.MyJsonLib aPrefix = new JsonLibs.MyJsonLib ();

					aPrefix.Add ("fiGroupName", (string)entri.Key);
					aPrefix.Add ("fiProducts", arPrfxPg);

					arPg.Add (aPrefix);
				}
			} else {
				for (int i = 0; i < hasil.Count; i++) {
					JsonLibs.MyJsonLib aPGroup = new JsonLibs.MyJsonLib ();
					aPGroup.Add ("fiItemCode", hasil [i].code);
					aPGroup.Add ("fiItemName", hasil [i].name);
					// jika produk, maka isi detailnya
					JsonLibs.MyJsonLib itemdetails = new JsonLibs.MyJsonLib ();
					if (list_code == 1) {
						itemdetails.Add ("fiGroupProductCode", hasil [i].group_code);
						itemdetails.Add ("fiGroupProductName", hasil [i].group_name);
						itemdetails.Add ("fiPrice", hasil [i].price);
						itemdetails.Add ("fiImageCover", hasil [i].image_cover);
						itemdetails.Add ("fiIsNegotiable", hasil [i].is_negotiable);
						itemdetails.Add ("fiDescription", hasil [i].description);
						itemdetails.Add ("fiSpecification", hasil [i].specification);
						itemdetails.Add ("fiTargetAge", hasil [i].target_age);
						itemdetails.Add ("fiBrand", hasil [i].brand);
						itemdetails.Add ("fiProductYear", hasil [i].product_year);
						itemdetails.Add ("fiIsNew", hasil [i].is_new);
						itemdetails.Add ("fiIsPersonal", hasil [i].is_personal);
						itemdetails.Add ("fiSellerName", hasil [i].seller_name);
						itemdetails.Add ("fiSellerLocation", hasil [i].seller_location);
						itemdetails.Add ("fiSellerPhone", hasil [i].seller_phone);
					}
					hasil [i].Dispose ();
					aPGroup.Add ("fiItemDetails", itemdetails);

					arPg.Add (aPGroup);
				}
			}


			jsonConv.Clear();
			jsonConv.Add ("fiApplicationId",appID);
			jsonConv.Add ("fiListCode", list_code);
			jsonConv.Add ("fiProductQueryList", arPg);
			jsonConv.Add ("fiResponseCode","00");

			string repjson = jsonConv.JSONConstruct ();
			arPg.Dispose ();
			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", repjson);
		}

		public string ShopProductCategoryInquiry(){
			string[] fields = { "fiApplicationId" };

			string appID = "";

			if (!jsonConv.JSONParse(clientData.Body))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
			}

			if(!checkMandatoryFields(fields))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory fields not found", "");
			}

			try
			{
				appID = ((string)jsonConv["fiApplicationId"]).Trim();
			}
			catch
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
			}

			Hashtable pcList = null;
			if (!localDB.getShopsProductCategory(appID, ref pcList, out xError)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Query Shops Product Category failed!", "");
			}

			JsonLibs.MyJsonArray arPc = new JsonLibs.MyJsonArray ();
			foreach (DictionaryEntry entry in pcList) {
				JsonLibs.MyJsonLib aPCategory = new JsonLibs.MyJsonLib ();
				aPCategory.Add ("fiProductCategoryId",(string)entry.Key);
				aPCategory.Add ("fiProductCategoryName", (string)entry.Value);
				arPc.Add (aPCategory);
			}

			jsonConv.Clear();
			jsonConv.Add ("fiApplicationId",appID);
			jsonConv.Add("fiProductCategoryList", arPc);
	
			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
		}

		public string ShopProductInquiry(){
			string[] fields = { "fiApplicationId","fiPreference", "fiIndexOffset", "fiIndexLength", "fiProductCategoryId" };

			string appID = "";

			if (!jsonConv.JSONParse(clientData.Body))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
			}

			if(!checkMandatoryFields(fields))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory fields not found", "");
			}

			try
			{
				appID = ((string)jsonConv["fiApplicationId"]).Trim();
			}
			catch
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
			}

			Hashtable pcList = null;
			if (!localDB.getShopsProductCategory(appID, ref pcList, out xError)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Query Shops Product Category failed!", "");
			}

			JsonLibs.MyJsonArray arPc = new JsonLibs.MyJsonArray ();
			foreach (DictionaryEntry entry in pcList) {
				JsonLibs.MyJsonLib aPCategory = new JsonLibs.MyJsonLib ();
				aPCategory.Add ("fiProductCategoryId",(string)entry.Key);
				aPCategory.Add ("fiProductCategoryName", (string)entry.Value);
				arPc.Add (aPCategory);
			}

			jsonConv.Clear();
			jsonConv.Add ("fiApplicationId",appID);
			jsonConv.Add("fiProductCategoryList", arPc);

			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
		}

	}
}

