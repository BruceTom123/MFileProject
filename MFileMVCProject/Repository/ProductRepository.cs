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
using System.Web;

namespace MFileMVCProject.Repository
{
    public class ProductRepository
    {
        private IList<Product> productList = new List<Product>();

        private System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();

        public AdminData userinfo = new AdminData();
        
        public List<User> users = new List<User>()
        {
            new User("vlad", "birthday"),
            new User("Harry", "harry"),
            new User("Nicolas", "nicolas"),
            new User("angel", "angel"),
            new User("bruce", "bruce"),
            new User("mfile", "mfile"),
        };

      
        public string AuthenticationToken { get; private set; }

        public ProductRepository()
        {
            SetUserinfo();
            GetAuthentication();
        }

        //confirm the login info
        public bool CheckAuth(string username, string password)
        {
            foreach (User user in users)
            {
                if (user.Username == username && user.Password == password)
                    return true;
            }
            return false;
        }

        //set the mfile working environment
        private void SetUserinfo()
        {
            string fileLoc = AppDomain.CurrentDomain.BaseDirectory + "Assets\\setting.bak";
            StreamReader sr = new StreamReader(fileLoc);
            string settingData = sr.ReadToEnd();
            userinfo = JsonConvert.DeserializeObject<AdminData>(settingData);
            sr.Close();
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

        private void GetAuthentication()
        {
            // Create a JSON.NET serializer to serialize/deserialize request and response bodies.
            var jsonSerializer = JsonSerializer.CreateDefault();

            // Create the authentication details.
            var auth = new
            {
                Username = userinfo.MUsername,
                Password = userinfo.MPassword,
                VaultGuid = userinfo.MVaultId // Use GUID format with {braces}.
            };

            // Create the web request.
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

        private async Task<int> GetProductsFromMFileAsync()
        {
            var url = new Uri(userinfo.BaseUrl + "REST/objects.aspx?o=" + userinfo.MProductTypeId.ToString());
            // Start the request.
            try
            {
                var responseBody = await client.GetStringAsync(url);        
                Product tempProduct;
                // Attempt to parse it.  For this we will use the Json.NET library, but you could use others.
                var results = JsonConvert.DeserializeObject<Results<ObjectVersion>>(responseBody);
                int count = results.Items.Count;
                foreach (ObjectVersion result in results.Items)
                {
                    tempProduct = new Product();
                    tempProduct.Title = result.Title;
                    tempProduct.TypeId = userinfo.MProductTypeId;
                    tempProduct.Id = result.ObjVer.ID;
                    //tempProduct.ProductCode = await GetProductCodeFromMFileAsync(tempProduct.TypeId, tempProduct.Id, userinfo.MProductCodeId);
                   // tempProduct.ProductExtendedName = await GetProductExtendNameFromMFileAsync(tempProduct.TypeId, tempProduct.Id, userinfo.MProductExtenedNameId);
                    productList.Add(tempProduct);
                    count++;
                }
                return count;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        public async Task SavePdftoMfile(List<Question> questions, string productTitle, string productId)
        {
            CreatePdfFile(questions, productTitle);
            await CreatFileInMFileAsync(productTitle, productId);
        }

        public async Task<string> GetProductExtendNameFromMFileAsync(int typeId, int id, int mProductExtenedNameId)
        {
            var url =
               new Uri(userinfo.BaseUrl + "REST/objects/" + String.Format("{0}/{1}", typeId, id) + "/latest/properties/" + mProductExtenedNameId.ToString());

            // Start the request.
            var responseBody = await client.GetStringAsync(url);

            // Attempt to parse it.  For this we will use the Json.NET library, but you could use others.
            var results = JsonConvert.DeserializeObject<TypedValue>(responseBody);
            string valueofTypedvalue = JsonConvert.SerializeObject(results.Value);
            //Console.WriteLine(valueofTypedvalue);
            ValueOfTypedvalue obj = JsonConvert.DeserializeObject<ValueOfTypedvalue>(valueofTypedvalue);
            return obj.Value;
        }

        public async Task<string> GetProductCodeFromMFileAsync(int typeId, int id, int mProductCodeId)
        {
            var url =
               new Uri(userinfo.BaseUrl + "REST/objects/" + String.Format("{0}/{1}", typeId, id) + "/latest/properties/" + mProductCodeId.ToString());

            // Start the request.
            var responseBody = await client.GetStringAsync(url);

            // Attempt to parse it.  For this we will use the Json.NET library, but you could use others.
            var results = JsonConvert.DeserializeObject<TypedValue>(responseBody);
            string valueofTypedvalue = JsonConvert.SerializeObject(results.Value);
            //Console.WriteLine(valueofTypedvalue);
            ValueOfTypedvalue obj = JsonConvert.DeserializeObject<ValueOfTypedvalue>(valueofTypedvalue);
            return obj.Value;
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
                    tempquestion = JsonConvert.DeserializeObject<Question>(questionString);
                    i++;
                    tempquestion.Sequence = i;
                    if(tempquestion.Id > 0)
                    {
                        var url =
                            new Uri(userinfo.BaseUrl + "REST/objects/0/" + tempquestion.Id + "/latest/files/" + tempquestion.Id + "/content");
                        
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
                    questionResults.Add(tempquestion);
                }
            }
            return questionResults;
        }

        private async Task<string> GetJsonDataFromMFileAsync(string productTitle)
        {
            var url = new Uri(userinfo.BaseUrl + "REST/objects.aspx?q=" + System.Net.WebUtility.UrlEncode(productTitle));

            // Start the request.
            var responseBody = await client.GetStringAsync(url);
            Product tempProduct;
            // Attempt to parse it.  For this we will use the Json.NET library, but you could use others.
            var results = JsonConvert.DeserializeObject<Results<ObjectVersion>>(responseBody);
            int count = results.Items.Count;
            WebRequest request;
            foreach (ObjectVersion result in results.Items)
            {
                tempProduct = new Product();
                tempProduct.Title = result.Title;
                tempProduct.Id = result.ObjVer.ID;
                if (!result.SingleFile) continue;
                if (result.Files[0].Extension != "json") continue;
                var jsonUrl =
                    new Uri(userinfo.BaseUrl + "REST/objects/0/" + result.ObjVer.ID.ToString() + "/latest/properties/" + userinfo.MProductPropertyId.ToString());

                // Start the request.
                responseBody = await client.GetStringAsync(jsonUrl);
                // Attempt to parse it.  For this we will use the Json.NET library, but you could use others.
                var typedResult = JsonConvert.DeserializeObject<PropertyValue>(responseBody);
                if (typedResult.TypedValue.DisplayValue != productTitle) continue;
                var fileurl = new Uri(userinfo.BaseUrl + "REST/objects/0/" + result.ObjVer.ID.ToString() + "/latest/files/" + result.ObjVer.ID.ToString() + "/content");
                // Create the web request.
                request = WebRequest.Create(fileurl);
                request.Headers["X-Authentication"] = this.AuthenticationToken;
                // Receive the response.
                var response = request.GetResponse();
                
                return new StreamReader(response.GetResponseStream()).ReadToEnd();
            }
            return "";
        }

        private void CreatePdfFile(List<Question> questions, string productTitle)
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
            table.AddCell(new PdfPCell(new Phrase(userinfo.MUsername, font)) { BorderColor = table1_border, HorizontalAlignment = 1 });               // the Username is  here
            table.AddCell(new PdfPCell(new Phrase("Mo", italicFnt)) { Border = PdfPCell.NO_BORDER, HorizontalAlignment = 2, PaddingRight = 10 });
            table.AddCell(new PdfPCell(new Phrase("Modata", font)) { BorderColor = table1_border, BackgroundColor = table1_Back, HorizontalAlignment = 1 }); // the Mo data is here
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
                para = new Paragraph("                 \t\t Serial\n", new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 14, iTextSharp.text.Font.BOLD | iTextSharp.text.Font.ITALIC));
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

        private async Task CreatFileInMFileAsync(string productTitle, string productId)
        {

            // Which file do we need to upload?
            string path = AppDomain.CurrentDomain.BaseDirectory + "Assets\\uploads\\test.pdf";
            var localFileToUpload = new System.IO.FileInfo(path);

            var uploadFileResponse = await client.PostAsync(new Uri(userinfo.BaseUrl + "REST/files.aspx"),
                new System.Net.Http.StreamContent(localFileToUpload.OpenRead()));

            // Extract the value.
            var uploadInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<UploadInfo>(
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
                        PropertyDef = 0, // The built-in "Name or Title" property Id.
			            TypedValue =  new TypedValue()
                        {
                            DataType = MFDataType.Text,
                            Value = userinfo.MUsername + "(" + productTitle + ")-" + DateTime.Now.ToShortDateString() // here the pdf file title decide
                        }
                    },
                    new PropertyValue()
                    {
                        PropertyDef = userinfo.MSerialId, // The Serial property Id.
			            TypedValue =  new TypedValue()
                        {
                            DataType = MFDataType.Text,
                            Value = DateTime.Now.ToShortDateString()   // here the serial data of pdf file is decided as you want.
                        }
                    }
                }
            };
            objectCreationInfo.Files = new UploadInfo[]
            {
                uploadInfo
            };
            // Serialise using JSON.NET (use Nuget to add a reference if needed).
            var stringContent = Newtonsoft.Json.JsonConvert.SerializeObject(objectCreationInfo);

            // Create the content for the web request.
            var content = new System.Net.Http.StringContent(stringContent, Encoding.UTF8, "application/json");

            // We are creating a document
            const int documentObjectTypeId = 0; //

            // Execute the POST.
            var httpResponseMessage = await client.PostAsync(new Uri(userinfo.BaseUrl + "REST/objects/" + documentObjectTypeId + ".aspx"), content);
            var objectVersion = Newtonsoft.Json.JsonConvert.DeserializeObject<ObjectVersion>(
                            await httpResponseMessage.Content.ReadAsStringAsync());
            string newId = objectVersion.ObjVer.ID.ToString();        
        }
    }
}