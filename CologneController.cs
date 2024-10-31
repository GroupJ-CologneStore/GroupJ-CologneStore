﻿using CologneStore.Constants;
using CologneStore.DTO;
using CologneStore.ImageService;
using CologneStore.Models;
using CologneStore.Repositories;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IO;

namespace CologneStore.Controllers
{
	[Authorize(Roles = nameof(Roles.Company))]
	public class CologneController : Controller
	{
		private readonly ICologneRepository _cologneRepository;
		private readonly ITypeRepository _typeRepository;
		private readonly ICologneForRepository _cologneForRepository;
		private readonly IFileService _fileService;

		

		public CologneController(ICologneRepository cologneRepository, IFileService fileService, ITypeRepository typeRepository, ICologneForRepository cologneForRepository)
		{
			_cologneRepository = cologneRepository;
			_typeRepository = typeRepository;
			_fileService = fileService;
			_cologneForRepository = cologneForRepository;
		}


		FirestoreDb firestoreDb = FirestoreDb.Create("cologne-store");


		public async Task<IActionResult> Index()
		{
			return View(await _cologneRepository.GetColognes());
		}

		public async Task<IActionResult> AddCologne()
		{
			var typeList = (await _typeRepository.GetAllTypes()).Select(type
			=> new SelectListItem
			{
				Text = type.TypeName,
				Value = type.Id.ToString()
			});

			var cologneList = (await _cologneForRepository.GetAllColognesFor()).Select(cologneFor
		   => new SelectListItem
		   {
			   Text = cologneFor.CologneForName,
			   Value = cologneFor.Id.ToString()
		   });

			CologneDTO cologneDTO = new CologneDTO()
			{
				TypeList = typeList,
				CologneForList = cologneList
			};

			return View(cologneDTO);
		}

		[HttpPost]
		public async Task<IActionResult> AddCologne(CologneDTO cologneDTO)
		{
			var typeList = (await _typeRepository.GetAllTypes()).Select(type
			=> new SelectListItem
			{
				Text = type.TypeName,
				Value = type.Id.ToString()
			});

			var cologneList = (await _cologneForRepository.GetAllColognesFor()).Select(cologneFor
		   => new SelectListItem
		   {
			   Text = cologneFor.CologneForName,
			   Value = cologneFor.Id.ToString()
		   });
			cologneDTO.TypeList = typeList;
			cologneDTO.CologneForList = cologneList;



			if (ModelState.IsValid)
				return View(cologneDTO);

			try
			{
				if (cologneDTO.ImageFile != null)
				{
					if (cologneDTO.ImageFile.Length > 1 * 1024 * 1024)
					{
						throw new InvalidOperationException("Image file cannot exceed 1MB");
					}
					string[] allowedExtensions = [".jpeg", ".jpg", ".png"];
					string imageName = await _fileService.SaveFile(cologneDTO.ImageFile, allowedExtensions);
					cologneDTO.CologneImage = imageName;
				}

				Cologne cologne = new Cologne()
				{
					Id = cologneDTO.Id,
					CologneName = cologneDTO.CologneName,
					CologneMakerName = cologneDTO.CologneMakerName,
					CologneImage = cologneDTO.CologneImage,
					TypeId = cologneDTO.TypeId,
					CologneForId = cologneDTO.CologneForId,
					Price = cologneDTO.Price,
				};

				await _cologneRepository.AddCologne(cologne);
				CollectionReference col = firestoreDb.Collection("Colognes");
				string id = col.Document().Id;
				string gender;

				if (cologneDTO.CologneForId == 1)
				{
					gender = "For Him";
				}
				else
				{
					gender = "For Her";
				}
				Dictionary<string, object> data = new Dictionary<string, object>()
				{
					{"id",id},
					{"name", cologneDTO.CologneName},
					{"brand", gender },
					{"typeId", cologneDTO.TypeId },
					{"cologneForId", cologneDTO.CologneForId },
					{"price", cologneDTO.Price },
					{"image",cologneDTO.CologneImage },
				};
				await col.AddAsync(data);
				TempData["successMessage"] = "Cologne is added successfully!";
				return RedirectToAction(nameof(Index));
			}
			catch (InvalidOperationException)
			{

				TempData["errorMessage"] = "Cologne has not been added";
				return View(cologneDTO);
			}
			catch (Exception)
			{
				TempData["successMessage"] = "Cologne is added successfully!";
				return View(cologneDTO);
			}
		}

		public async Task<IActionResult> UpdateCologne(int id)
		{
			var cologne = await _cologneRepository.GetCologneById(id);
			if (cologne == null)
			{
				TempData["errorMessage"] = $"Cologne with the id: {id} was not found";
				RedirectToAction(nameof(Index));
			}

			var typeList = (await _typeRepository.GetAllTypes()).Select(type =>
			new SelectListItem
			{
				Text = type.TypeName,
				Value = type.Id.ToString(),
				Selected = type.Id == cologne.TypeId
			});

			var cologneList = (await _cologneForRepository.GetAllColognesFor()).Select(cologneFor
		   => new SelectListItem
		   {
			   Text = cologneFor.CologneForName,
			   Value = cologneFor.Id.ToString()
		   });

			CologneDTO cologneDTO = new CologneDTO()
			{
				TypeList = typeList,
				CologneForList = cologneList,
				CologneName = cologne.CologneName,
				CologneMakerName = cologne.CologneMakerName,
				TypeId = cologne.TypeId,
				CologneForId = cologne.CologneForId,
				Price = cologne.Price,
			};

			return View(cologneDTO);
		}

		[HttpPost]
		public async Task<IActionResult> UpdateCologne(CologneDTO cologneDTO)
		{
			var typeList = (await _typeRepository.GetAllTypes()).Select(type =>
			new SelectListItem
			{
				Text = type.TypeName,
				Value = type.Id.ToString(),
				Selected = type.Id == cologneDTO.TypeId
			});

			var cologneList = (await _cologneForRepository.GetAllColognesFor()).Select(cologneFor
		   => new SelectListItem
		   {
			   Text = cologneFor.CologneForName,
			   Value = cologneFor.Id.ToString()
		   });

			cologneDTO.TypeList = typeList;
			cologneDTO.CologneForList = cologneList;

			if (ModelState.IsValid)
			{
				return View(cologneDTO);
			}

			try
			{
				string oldImage = "";
				if (cologneDTO.ImageFile != null)
				{
					if (cologneDTO.ImageFile.Length > 1 * 1024 * 1024)
					{
						throw new InvalidOperationException($"Image file cannot exceed 1MB");
					}
					string[] allowedExtensions = [".jpeg", ".jpg", ".png"];
					string imageName = await _fileService.SaveFile(cologneDTO.ImageFile, allowedExtensions);

					oldImage = cologneDTO.CologneImage;
					cologneDTO.CologneImage = imageName;
				}

				Cologne cologne = new Cologne()
				{
					Id = cologneDTO.Id,
					CologneName = cologneDTO.CologneName,
					CologneMakerName = cologneDTO.CologneMakerName,
					TypeId = cologneDTO.TypeId,
					CologneForId = cologneDTO.CologneForId,
					Price = cologneDTO.Price,
					CologneImage = cologneDTO.CologneImage,
				};

				CollectionReference col = firestoreDb.Collection("Colognes");
				Dictionary<string, object> data = new Dictionary<string, object>()
				{
				    //{"id",id},
				    {"name", cologneDTO.CologneName},
					{"brand", cologneDTO.CologneMakerName },
					{"typeId", cologneDTO.TypeId },
					{"cologneForId", cologneDTO.CologneForId },
					{"price", cologneDTO.Price }
				};
				await col.Document("5HW6yYufJyYU3z3uo61e").UpdateAsync(data);

				await _cologneRepository.UpdateCologne(cologne);

				if (!string.IsNullOrWhiteSpace(oldImage))
				{
					_fileService.DeleteFile(oldImage);
				}
				TempData["successMessage"] = "Cologne is updated successfully!";
				return RedirectToAction("Index");
			}


			catch (InvalidOperationException ex)
			{

				TempData["errorMessage"] = ex.Message;
				return View(cologneDTO);
			}

		}

		public async Task<IActionResult> DeleteCologne(int id)
		{
			try
			{
				var cologne = await _cologneRepository.GetCologneById(id);
				if (cologne == null)
				{
					TempData["errormessage"] = $"Cologne with id: {id} was not found";
					return View(cologne);
				}
				else
				{
					await _cologneRepository.DeleteCologne(cologne);
					//if (!string.IsNullOrEmpty(cologne.CologneImage))
					//{
					//	_fileService.DeleteFile(cologne.CologneImage);
					//}
					TempData["successMessage"] = "Cologne successfully deleted";
					return RedirectToAction("Index");

				}
			}
			catch (InvalidOperationException ex)
			{

				throw;
			}
		}

	}
}