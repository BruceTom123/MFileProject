using iTextSharp.text;
using iTextSharp.text.pdf;
using MFaaP.MFWSClient;
using MFileMVCProject.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web;

namespace MFileMVCProject.Repository
{
    public class ProductRepository
    {
        private IList<Product> productList = new List<Product>(); //the product array from m-file

        private System.Net.Http.HttpClient client = new System.Net.Http.HttpClient(); //the httpclient to connect with m-file

        public AdminData userinfo = new AdminData() { };
      
        public string AuthenticationToken { get; set; } // auth-token
        public bool CheckAuthResult { get; set; }  //auth-connect-result

        public ProductRepository()
        {
            SetUserinfo();
            GetAuthentication();
        }

        //set the mfile working environment
        private void SetUserinfo()
        {
            string fileLoc = AppDomain.CurrentDomain.BaseDirectory + "Assets\\setting.bak";
            if (!File.Exists(fileLoc)) //if file doesn't exist, create one!
            {
                string tempdata = JsonConvert.SerializeObject(new AdminData());
                using (StreamWriter sw = new StreamWriter(fileLoc))
                {
                    sw.Write(tempdata);
                    sw.Close();
                }
                userinfo = new AdminData();
                return;
            } 
            StreamReader sr = new StreamReader(fileLoc);
            string settingData = sr.ReadToEnd();
            userinfo = JsonConvert.DeserializeObject<AdminData>(settingData);
            sr.Close();
            if (userinfo is null || String.IsNullOrEmpty(userinfo.BaseUrl)) return;
            if(userinfo.BaseUrl.Substring(userinfo.BaseUrl.Length - 1, 1) != "/")
            {
                userinfo.BaseUrl += "/";
            }
        }

        public async Task SetProductListAsync()
        {
            await GetProductsFromMFileAsync();
        }

        public IList<Product> GetProducts()
        {            
            return productList;
        }

        public bool GetAuthentication()
        {
            CheckAuthResult = false;
            // Create a JSON.NET serializer to serialize/deserialize request and response bodies.
            var jsonSerializer = JsonSerializer.CreateDefault();

            if (userinfo is null)
                return false;
            // Create the authentication details.
            var auth = new
            {
                Username = userinfo.MUsername,
                Password = userinfo.MPassword,
                VaultGuid = userinfo.MVaultId // Use GUID format with {braces}.
            };           
            try
            {
                var authenticationRequest = (HttpWebRequest)WebRequest.Create(userinfo.BaseUrl + "REST/server/authenticationtokens.aspx");
                 authenticationRequest.Method = "POST";
                 // Add the authentication details to the request stream.
                using (var streamWriter = new StreamWriter(authenticationRequest.GetRequestStream()))
                {
                    using (var jsonTextWriter = new JsonTextWriter(streamWriter))
                    {
                        jsonSerializer.Serialize(jsonTextWriter, auth);
                    }
                }
                // Execute the request.
                var authenticationResponse = (HttpWebResponse)authenticationRequest.GetResponse();

                // Extract the authentication token.
                string authenticationToken = null;
                using (var streamReader = new StreamReader(authenticationResponse.GetResponseStream()))
                {
                    using (var jsonTextReader = new JsonTextReader(streamReader))
                    {
                        authenticationToken = ((dynamic)jsonSerializer.Deserialize(jsonTextReader)).Value;
                    }
                }
                client.DefaultRequestHeaders.Add("X-Authentication", authenticationToken);
                AuthenticationToken = authenticationToken;
            }
            catch(Exception ex)
            {
                return false;
            }
            CheckAuthResult = true;
            return true;
        }

        public async Task<List<string>> GetModataAsync(string productId)
        {
            var url = new Uri(userinfo.BaseUrl + "REST/objects/" + userinfo.MProductTypeId.ToString() +"/" + productId + "/latest/relationships");
            List<string> modatas = new List<string>();

            if (userinfo.MOrderTypeId == 0)
                return modatas;
            // Start the request.
            try
            {
                var responseBody = await client.GetStringAsync(url);
                responseBody = "{items:" + responseBody + "}";
                var results = JsonConvert.DeserializeObject<Results<ObjectVersion>>(responseBody);
                foreach(ObjectVersion result in results.Items)
                {
                    if (result.ObjVer.Type == userinfo.MOrderTypeId)
                        modatas.Add(result.Title);
                }
            }
            catch(Exception ex)
            {

            }            
            return modatas;
        }

        private async Task GetProductsFromMFileAsync()
        {

            if (!CheckAuthResult)   //If auth is wrong, empty result send
                return;
            var url = new Uri(userinfo.BaseUrl + "REST/objects.aspx?q=json");
            Product tempProduct;
            var responseBody = await client.GetStringAsync(url);   
            // Start the request.
            try
            {                                    
                // Attempt to parse it.  For this we will use the Json.NET library, but you could use others.
                var results = JsonConvert.DeserializeObject<Results<ObjectVersion>>(responseBody);
                foreach (ObjectVersion result in results.Items)
                {
                    var jsonUrl = new Uri(userinfo.BaseUrl + "REST/objects/0/" + result.ObjVer.ID.ToString() + "/latest/properties/" + 
                        userinfo.MProductPropertyId.ToString());
                    responseBody = await client.GetStringAsync(jsonUrl);
                    var proPropertyJson = JsonConvert.DeserializeObject<PropertyValue>(responseBody);
                    tempProduct = new Product() { Id = proPropertyJson.TypedValue.Lookups[0].Item, Title = proPropertyJson.TypedValue.DisplayValue, TypeId = userinfo.MProductTypeId };
                    if (tempProduct is null) continue;
                    productList.Add(tempProduct);
                }
            }
            catch (Exception ex)
            {
                return;
            }
        }

        public async Task<bool> SavePdftoMfile(List<Question> questions, string productTitle, string productId, string modata, string serial, string username)
        {
            CreatePdfFile(questions, productTitle, modata, serial, username);
            bool result = await CreatFileInMFileAsync(productTitle, productId, serial);
            return result;
        }

        public async Task<string> GetProductExtendNameFromMFileAsync(int typeId, int id, int mProductExtenedNameId)
        {
            var url =
               new Uri(userinfo.BaseUrl + "REST/objects/" + String.Format("{0}/{1}", typeId, id) + "/latest/properties/" + mProductExtenedNameId.ToString());
            try
            {
                var responseBody = await client.GetStringAsync(url);

                // Attempt to parse it.  For this we will use the Json.NET library, but you could use others.
                var results = JsonConvert.DeserializeObject<TypedValue>(responseBody);
                string valueofTypedvalue = JsonConvert.SerializeObject(results.Value);
                //Console.WriteLine(valueofTypedvalue);
                ValueOfTypedvalue obj = JsonConvert.DeserializeObject<ValueOfTypedvalue>(valueofTypedvalue);
                return obj.Value;
            }
            catch(Exception ex)
            {
                
            }
            // Start the request.
            return "Not correct ProductExtendName PropertyId";
        }

        public async Task<string> GetProductCodeFromMFileAsync(int typeId, int id, int mProductCodeId)
        {
            var url =
               new Uri(userinfo.BaseUrl + "REST/objects/" + String.Format("{0}/{1}", typeId, id) + "/latest/properties/" + mProductCodeId.ToString());

            try
            {
                // Start the request.
                var responseBody = await client.GetStringAsync(url);

                // Attempt to parse it.  For this we will use the Json.NET library, but you could use others.
                var results = JsonConvert.DeserializeObject<TypedValue>(responseBody);
                string valueofTypedvalue = JsonConvert.SerializeObject(results.Value);
                //Console.WriteLine(valueofTypedvalue);
                ValueOfTypedvalue obj = JsonConvert.DeserializeObject<ValueOfTypedvalue>(valueofTypedvalue);
                return obj.Value;
            }
            catch(Exception ex)
            {

            }
            return "Not correct ProductCode PropertyId";

        }

        public async Task<List<Question>> GetQuestionFromProductAsync(string productTitle)
        {
            
            string jsonData = await GetJsonDataFromMFileAsync(productTitle);
            List<Question> questionResults = new List<Question>();
            Question tempquestion;
            WebRequest request;

            var tempstrings = jsonData.Split('{');
            int i = 0;
            foreach (String tempString in tempstrings)
            {
                if (tempString.IndexOf("}") > 0)
                {
                    string questionString = "{" + tempString.Substring(0, tempString.IndexOf("}")) + "}";
                    tempquestion = JsonConvert.DeserializeObject<Question>(questionString);  //each question
                    if(tempquestion.Type is null || tempquestion.Caption is null)
                    {
                        continue;
                    }
                    i++;
                    tempquestion.Sequence = i;
                    if(tempquestion.Id > 0) //case of image
                    {
                        var url =
                            new Uri(userinfo.BaseUrl + "REST/objects/0/" + tempquestion.Id + "/latest");
                        
                        try
                        {
                            var responsebody = await client.GetStringAsync(url);
                            int fileId = JsonConvert.DeserializeObject<ObjectVersion>(responsebody).Files[0].ID;
                            url =
                                new Uri(userinfo.BaseUrl + "REST/objects/0/" + tempquestion.Id + "/latest/files/" + fileId + "/content");

                            request = WebRequest.Create(url);
                            request.Headers["X-Authentication"] = AuthenticationToken;
                            // Receive the response.
                            var response = request.GetResponse();
                            Stream stream = response.GetResponseStream();
                            Bitmap bitmap;
                            bitmap = new Bitmap(stream);
                            string path = AppDomain.CurrentDomain.BaseDirectory + "Assets\\img\\" + tempquestion.Sequence.ToString() + ".png";
                            if (bitmap != null)
                                bitmap.Save(path);
                            stream.Flush();
                            stream.Close();
                        }
                        catch(Exception ex)
                        {
                            
                        }
                        
                    }
                    questionResults.Add(tempquestion);
                }
            }
            return questionResults;
        }

        private async Task<string> GetJsonDataFromMFileAsync(string productTitle)
        {
            var url = new Uri(userinfo.BaseUrl + "REST/objects.aspx?q=" + WebUtility.UrlEncode(productTitle));

            // Start the request.
            var responseBody = await client.GetStringAsync(url);

            // Attempt to parse it.  For this we will use the Json.NET library, but you could use others.
            var results = JsonConvert.DeserializeObject<Results<ObjectVersion>>(responseBody);
            int count = results.Items.Count;
            WebRequest request;
            foreach (ObjectVersion result in results.Items)
            {
                if (!result.SingleFile) continue;
                if (result.Files[0].Extension != "json") continue;
                var jsonUrl =
                    new Uri(userinfo.BaseUrl + "REST/objects/0/" + result.ObjVer.ID.ToString() + "/latest/properties/" + userinfo.MProductPropertyId.ToString());

                // Start the request.
                try
                {
                    responseBody = await client.GetStringAsync(jsonUrl);
                    // Attempt to parse it.  For this we will use the Json.NET library, but you could use others.
                    var typedResult = JsonConvert.DeserializeObject<PropertyValue>(responseBody);
                    if (typedResult.TypedValue.DisplayValue != productTitle) continue;
                    var fileurl = new Uri(userinfo.BaseUrl + "REST/objects/0/" + result.ObjVer.ID.ToString() + "/latest/files/" + result.Files[0].ID.ToString() + "/content");
                    // Create the web request.
                    request = WebRequest.Create(fileurl);
                    request.Headers["X-Authentication"] = AuthenticationToken;
                    // Receive the response.
                    var response = request.GetResponse();
                
                    return new StreamReader(response.GetResponseStream()).ReadToEnd();
                }
                catch(Exception ex)
                {
                    return "";
                }
                
            }
            return "";
        }

        private void CreatePdfFile(List<Question> questions, string productTitle, string modata, string serial, string username)
        {            
            string path = AppDomain.CurrentDomain.BaseDirectory + "Assets\\uploads\\test.pdf";
            
            //Pdf source file is made here
            Document doc = new Document(PageSize.A4, 10f, 10f, 10f, 0f);
            var titleFont = FontFactory.GetFont("Tahoma", 25, iTextSharp.text.Font.BOLD);
            BaseFont bf = BaseFont.CreateFont(
                         BaseFont.TIMES_ROMAN,
                         BaseFont.CP1252,
                         BaseFont.EMBEDDED);
            iTextSharp.text.Font font = new iTextSharp.text.Font(bf, 13);
            var italicFnt = FontFactory.GetFont("Tahoma", 11, iTextSharp.text.Font.ITALIC);
            var tableHead = FontFactory.GetFont("TIMES_ROMAN", 18, iTextSharp.text.Font.BOLD, BaseColor.WHITE);
            var secTabelFont = FontFactory.GetFont("TIMES_ROMAN", 10, iTextSharp.text.Font.BOLD, BaseColor.BLACK);

            BaseColor table1_border = new BaseColor(142, 170, 219);
            BaseColor table1_Back = new BaseColor(217, 226, 243);
            BaseColor table2_border = new BaseColor(91, 155, 213);
            BaseColor table2_back = new BaseColor(222, 234, 246);

            PdfPTable table = new PdfPTable(2);
            table.WidthPercentage = 85f;
            int[] firstTablecellwidth = { 15, 85 };
            table.SetWidths(firstTablecellwidth);
            table.AddCell(new PdfPCell(new Phrase("Date/Time", italicFnt)) { Border = PdfPCell.TOP_BORDER, HorizontalAlignment = 2, BorderColor = table1_border, PaddingRight = 10 });
            table.AddCell(new PdfPCell(new Phrase(DateTime.Now.ToString())) { BorderColor = table1_border, BackgroundColor = table1_Back, HorizontalAlignment = 1 });
            table.AddCell(new PdfPCell(new Phrase("User", italicFnt)) { Border = PdfPCell.NO_BORDER, HorizontalAlignment = 2, PaddingRight = 10 });
            table.AddCell(new PdfPCell(new Phrase(username, font)) { BorderColor = table1_border, HorizontalAlignment = 1 });               // the Username is  here
            table.AddCell(new PdfPCell(new Phrase("Mo", italicFnt)) { Border = PdfPCell.NO_BORDER, HorizontalAlignment = 2, PaddingRight = 10 });
            table.AddCell(new PdfPCell(new Phrase(modata, font)) { BorderColor = table1_border, BackgroundColor = table1_Back, HorizontalAlignment = 1 }); // the Mo data is here
            table.AddCell(new PdfPCell(new Phrase("Product", italicFnt)) { Border = PdfPCell.NO_BORDER, HorizontalAlignment = 2, PaddingRight = 10 });
            table.AddCell(new PdfPCell(new Phrase(productTitle, font)) { BorderColor = table1_border, HorizontalAlignment = 1 });        // the Product Name is here.

            PdfPTable table2 = new PdfPTable(2);
            table2.WidthPercentage = 85f;
            int[] secTablecellwidth = { 17, 83 };
            table2.SetWidths(secTablecellwidth);
            table2.AddCell(new PdfPCell(new Phrase("QC Item", tableHead)) { BackgroundColor = table2_border, HorizontalAlignment = 1, BorderColor = table2_border, PaddingBottom = 20 });
            table2.AddCell(new PdfPCell(new Phrase("Result", tableHead)) { BorderColor = table2_border, BackgroundColor = table2_border, HorizontalAlignment = 0, PaddingLeft = 15 });
            int count = 0;
            foreach(Question question in questions)
            {
                //the caption name is here
                if(count%2 == 0)
                    table2.AddCell(new PdfPCell(new Phrase(question.Caption, secTabelFont)) { HorizontalAlignment = 1, BackgroundColor = table2_back,
                        BorderColor = table2_border, VerticalAlignment = 0, MinimumHeight = 55 });         
                else
                    table2.AddCell(new PdfPCell(new Phrase(question.Caption, secTabelFont)) { HorizontalAlignment = 1, BorderColor = table2_border,
                        VerticalAlignment = 0, MinimumHeight = 55 });

                string secCell = question.Value;
                if (secCell.ToString() == "true".ToString())
                    secCell = "Yes";
                if (secCell.ToString() == "false".ToString())
                    secCell = "No";
                if (question.Type == "Picture")      // here the image picture is put.(inside pdf file, in table)
                {
                    string imgPath = AppDomain.CurrentDomain.BaseDirectory + "Assets\\img\\" + secCell;
                    var image = iTextSharp.text.Image.GetInstance(imgPath);
                    if (image is null) continue;


                    image.ScaleToFit(260f, 300.25f);
                    var imageCell = new PdfPCell(image);
                    if (count % 2 == 0)
                        imageCell.BackgroundColor = table2_back;
                    float fixedHeight = 200f;
                    imageCell.FixedHeight = fixedHeight;
                    imageCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    imageCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    imageCell.BorderColor = table2_border;
                    
                    table2.AddCell(imageCell);
                }
                else
                {
                    if (count % 2 == 0)
                        table2.AddCell(new PdfPCell(new Phrase(secCell, font)) { BorderColor = table2_border, HorizontalAlignment = 0, BackgroundColor = table2_back, PaddingLeft = 15 });
                    else
                        table2.AddCell(new PdfPCell(new Phrase(secCell, font)) { BorderColor = table2_border, HorizontalAlignment = 0, PaddingLeft = 15 });
                }
                count++;
            }
            try
            {
                PdfWriter.GetInstance(doc, new FileStream(path, FileMode.Create));
                doc.Open();
                Paragraph para = new Paragraph("\nQC Form\n\n", new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 30));
                para.Alignment = Element.ALIGN_CENTER;

                doc.Add(para);
                para = new Paragraph("                 \t\t Serial                                                                                    " + serial + "\n" , 
                    new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 14, iTextSharp.text.Font.BOLD | iTextSharp.text.Font.ITALIC));
                doc.Add(para);
                para = new Paragraph(" ", new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 4));
                doc.Add(para);
                doc.Add(table);
                doc.Add(new Paragraph("\n\n\n"));
                doc.Add(table2);
            }
            catch (Exception ex)
            {

            }
            finally
            {
                doc.Close();
            }

        }

        private async Task<bool> CreatFileInMFileAsync(string productTitle, string productId, string serial)
        {

            // Which file do we need to upload?
            string path = AppDomain.CurrentDomain.BaseDirectory + "Assets\\uploads\\test.pdf";
            var localFileToUpload = new System.IO.FileInfo(path);

            var uploadFileResponse = await client.PostAsync(new Uri(userinfo.BaseUrl + "REST/files.aspx"),
                new System.Net.Http.StreamContent(localFileToUpload.OpenRead()));

            // Extract the value.
            var uploadInfo = JsonConvert.DeserializeObject<UploadInfo>(
                await uploadFileResponse.Content.ReadAsStringAsync());

            // Ensure the extension is set.
            // NOTE: This must be without the dot!
            uploadInfo.Extension = localFileToUpload.Extension.Substring(1);

            // Add the upload to the objectCreationInfo.
            var objectCreationInfo = new ObjectCreationInfo()
            {
                PropertyValues = new[]
                {
                    new PropertyValue()
                    {
                        PropertyDef = 100, // The built-in "Class" property Id.
			            TypedValue = new TypedValue()
                        {
                            DataType = MFDataType.Lookup,
                            Lookup = new Lookup()
                            {
                                Item = userinfo.MQCReportId, // The built-in "QC Report" class Id.
					            Version = -1 // Work around the bug detailed below.                                
                            }
                        }
                    },
                    new PropertyValue()
                    {
                        PropertyDef = userinfo.MProductPropertyId, // The built-in "Class" property Id.
			            TypedValue = new TypedValue()
                        {
                            DataType = MFDataType.MultiSelectLookup,
                            Lookups = new List<Lookup>()
                            {
                                new Lookup()
                                {
                                    Item = int.Parse(productId),
                                    Version = -1,
                                    DisplayValue = productTitle
                                }
                            }
                        }
                    },
                    new PropertyValue()
                    {
                        PropertyDef = 0, // The built-in "Name or Title" property Id.
			            TypedValue =  new TypedValue()
                        {
                            DataType = MFDataType.Text,
                            Value = serial // here the pdf file title decide
                        }
                    },
                    new PropertyValue()
                    {
                        PropertyDef = userinfo.MSerialId, // The Serial property Id.
			            TypedValue =  new TypedValue()
                        {
                            DataType = MFDataType.Text,
                            Value = serial   // here the serial of pdf file is decided as you want.
                        }
                    }
                }
            };

            objectCreationInfo.Files = new UploadInfo[]
            {
                uploadInfo
            };
            // Serialise using JSON.NET (use Nuget to add a reference if needed).
            var stringContent = JsonConvert.SerializeObject(objectCreationInfo);

            // Create the content for the web request.
            var content = new System.Net.Http.StringContent(stringContent, Encoding.UTF8, "application/json");

            // We are creating a document
            const int documentObjectTypeId = 0; //

            // Execute the POST.
            var httpResponseMessage = await client.PostAsync(new Uri(userinfo.BaseUrl + "REST/objects/" + documentObjectTypeId + ".aspx"), content);
            // Extract the value.
            var objectVersion = JsonConvert.DeserializeObject<ObjectVersion>(
                await httpResponseMessage.Content.ReadAsStringAsync());
            try
            {
                int fileId = objectVersion.ObjVer.ID;
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}