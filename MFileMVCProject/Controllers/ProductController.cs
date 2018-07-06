
using MFileMVCProject.Models;
using MFileMVCProject.Repository;
using PagedList;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MFileMVCProject.Controllers
{
    public class ProductController : Controller
    {
        private readonly ProductRepository productRepository = new ProductRepository();
        private List<Question> questions = new List<Question>();
        public async Task<ActionResult> Index(string sortOrder, string currentFilter, string searchString, int? page)
        {
            if (string.IsNullOrEmpty(Session["Username"] as string))
            {
                return RedirectToRoute("Login");
            }
            await productRepository.SetProductListAsync();
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "title_desc" : "Title";
            ViewBag.NameSortParm = sortOrder=="Title" ? "title_desc" : "Title";
            ViewBag.Message = "";

            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewBag.CurrentFilter = searchString;

            if (productRepository.GetProducts().Count == 0)  //when the mserver info is wrong, error message occures
                ViewBag.Message = "Your m-file server info is wrong.";

            var viewProductList = from s in productRepository.GetProducts() select s;
            if (!String.IsNullOrEmpty(searchString))
            {
                viewProductList = viewProductList.Where(s => s.Title.ToLower().Contains(searchString.ToLower()));
            }
            switch (sortOrder)
            {
                case "title_desc":
                    viewProductList = viewProductList.OrderByDescending(s => s.Title);
                    break;
                case "Title":
                    viewProductList = viewProductList.OrderBy(s => s.Title);
                    break;
                default:  // Name ascending 
                    viewProductList = viewProductList.OrderBy(s => s.Id);
                    break;
            }

            int pageSize = 13;
            int pageNumber = (page ?? 1);
            int i = 0;
            foreach(Product product in viewProductList)
            {
                i++;
                product.DisplayNumber = i;
            }
            return View(viewProductList.ToPagedList(pageNumber, pageSize));
        }

        public async Task<ActionResult> Detail(int id)
        {
            await productRepository.SetProductListAsync();
            var viewProductList = from s in productRepository.GetProducts() select s;
            Product detailedProduct = viewProductList.Where(m => m.Id == id).FirstOrDefault();
            detailedProduct.ProductCode = await productRepository.GetProductCodeFromMFileAsync(productRepository.userinfo.MProductTypeId, detailedProduct.Id,
                            productRepository.userinfo.MProductCodeId);
            detailedProduct.ProductExtendedName = await productRepository.GetProductExtendNameFromMFileAsync(productRepository.userinfo.MProductTypeId, detailedProduct.Id,
                            productRepository.userinfo.MProductExtenedNameId);
            return View(detailedProduct);
        }

        [HttpPost]
        public ActionResult Detail( Product product )
        {
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Question(string productTitle, string productId)
        {
            string title = productTitle.Trim();
            string proId = productId.Trim();
            questions = await productRepository.GetQuestionFromProductAsync(productTitle.Trim());
            if (questions.Count == 0)
                return RedirectToAction("Index");
            Session["ProductTitle"] = productTitle.Trim();
            Session["ProductId"] = productId.Trim();
            return View(questions);
        }

        [HttpPost]
        public async Task<ActionResult> Question()
        {
            string productTitle = Session["ProductTitle"].ToString();
            string productId = Session["ProductId"].ToString();
            questions = await productRepository.GetQuestionFromProductAsync(productTitle);
            if(questions.Count == 0) return RedirectToRoute("Products");
            string[] keys = Request.Form.AllKeys;
            var value = "";
            for (int i = 0; i < keys.Length; i++)
            {
                string keyname = keys[i];
                if (keyname.IndexOf("value") != 0) continue;
                value = Request.Form[keys[i]].ToString();
                int index = Int32.Parse(keyname.Split('-')[1]);
                questions[index - 1].Value = value;
            }
            await productRepository.SavePdftoMfile(questions, productTitle, productId);
            return RedirectToRoute("Products");
        }

        [HttpPost]
        public ActionResult ImageUpload()
        {         
            if (Request.Files.Count > 0)
            {
                try
                { 
                    //  Get all files from Request object  
                    HttpFileCollectionBase files = Request.Files;
                    for (int i = 0; i < files.Count; i++)
                    {

                        HttpPostedFileBase file = files[i];
                      
                           string fname ="upload-" + Request["username"] + ".png";

                        // Get the complete folder path and store the file inside it.  
                        fname = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "Assets\\img\\", fname);
                        file.SaveAs(fname);
                    }
                    // Returns message that successfully uploaded  
                    return Json("File Uploaded Successfully!");
                }
                catch (Exception ex)
                {
                    return Json("Error occurred. Error details: " + ex.Message);
                }
            }
            else
            {
                return Json("No files selected.");
            }
        }
    }
}