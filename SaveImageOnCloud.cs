//Install CloudinaryDotNet package
namespace BookHaven.Web.Controllers
{
	[Authorize(Roles = AppRoles.Achieve)]                    
	public class BooksController : Controller
	{
		private readonly List<string> ImgMaxAllowdExtension = new() { ".jpg", ".jpeg", ".png" };
		private readonly int ImgMaxAllowdSize = 2097152; //2MB
		private readonly Cloudinary _cloudinary;    //To Access services cloudinary                         
													//IOptions<> To read data that binding from section to class
		public BooksController(IOptions<CloudinarySettings> cloudinarySettings)
		{
			Account account = new Account()
			{
				Cloud= cloudinarySettings.Value.CloudName,
				ApiKey= cloudinarySettings.Value.APIKey,
				ApiSecret= cloudinarySettings.Value.APISecrect
			};
			_cloudinary = new Cloudinary(account);  //pass account to cloudinary
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(BookFormVM model)
		{
			if (BookIsExitsted != null && AuthorIsExitsted != null)
			{
				TempData["ErrorMessage"] = "The book with the same Author that you try to add existed!";   //SweetAlert
				SelectedList();
				return View();
			}
			string imgPublicId = null;
			if (model.Img != null)
			{
				var Extension = Path.GetExtension(model.Img.FileName); //.jpg, .jpeg, .png
				if (!ImgMaxAllowdExtension.Contains(Extension))
				{
					ModelState.AddModelError(nameof(model.ImgUrl), "Allowed only image with extension .jpg, .jpeg, .png");
					SelectedList();
					return View();
				}
				if (model.FormImg.Length > ImgMaxAllowdSize)
				{
					ModelState.AddModelError(nameof(model.ImgUrl), "Allowed only image with size 2:MB");
					SelectedList();
					return View();
				}

				// Delete Old image in state edit
				if (model.ImgUrl != null)
				{
					_cloudinary.DeleteResourcesAsync(book.ImgPublicId);
				}
				//Start Save To Cloudinary
				string RootPath = _webHostEnvironment.WebRootPath; //...wwwroot
				var ImageName = $"{Guid.NewGuid()}{Extension}";  //[random name] /3456sd23rf.png(generate GUID To be uninq in db) 
				var stream = Image.OpenReadStream(); //make stream for image to pass it to cloudinary
				var ImageParams = new ImageUploadParams() // هتحط هنا الفايل اللي عايز تبعته بالخصائص بتعته زي طوله عرضه حجمه إلخ
				{
					File = new FileDescription(ImageName, stream)
				};
				var UrlParams = await _cloudinary.UploadAsync(ImageParams); //upload url for params to cloudinary
				model.ImgUrl = UrlParams.SecureUrl.ToString();  // set url from cloudinary to database
				imgPublicId = UrlParams.PublicId;
				//End Save To Cloudinary
			}
			 model.ImgPublicId = imgPublicId; //save publicId for delete
			 var book = _mapper.Map<Book>(model);
			_unitOfWord.Books.Create(book);
			_unitOfWord.Commit();
			TempData["SuccessMessage"] = "Saved successfully";
			return RedirectToAction(nameof(Details), new {id=book.Id});
		}
	}
}
