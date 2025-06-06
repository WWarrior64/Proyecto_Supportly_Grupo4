// CargarController.cs
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Dropbox.Api;
using Dropbox.Api.Files;
using System.Threading.Tasks;

namespace Proyecto_Supportly.Controllers
{
    public class CargarController : Controller
    {
        // <-- Reemplaza aquí con tu token de Dropbox (mejor guardarlo en appsettings.json o variable de entorno)
        private const string _dropboxApiToken = "sl.u.AFxR-lyw0iXMpW26fl9ntnST1bgZTm5pZcu0u_gIiZaBLK44cHmoeAJ0m512y9eQtyp9kIAi2SPZtNMMV3uWNcwyQk4KHZgBlLKo-Tbi18vUwL9CHJcoK1FDaENULZVILi4VMr92CqS91GYFRl44H6I-RuoxzZePLi-YPJRaFyWcRna0OjePAyLuJ5PbumUysg1CX0ym5XKalEecz2CTjtNfscbvovBiXRlRz7ya60hh6U1LxgQa1We6eJMjIfH7hUrBNtBCDqaSKeYqbgWXXKqrhHr9DcHIpVI-sj8SUWhhx14cC5Jyp_upm0Jul54TNCCgfuD1YSCBhFWWJy-ykn2qiP3w_d9r7rbm4Y-uLQja__KKa6WO-MBz_UJ7PkHCtinOAR76QQ5CvIKc3O3ED0Wz-OzKt3ycS61kSnhrqQYgvXYbUeYdrCF8VG7KUNPT1bGtd415djjY4RpbI2RE3xqj-HOnEHLW7jyAJVWUHchuhD5UDAg226dYo1DWriKAAmwpg5dkBwTOHlvRKQiVoTlzTHEXQAN69bsOZiRKCwqXZ3g-AWHn-HrvA9VEzY9wyku2uMXxMrBZhmDB9EaslnvwCCNHFCki9tnPcki-ylfPGmHcN22RY3Nhvpohq_w4YNVd5teqjgUULvytkWvXkaynj3O5rflfTjIKB3p4pgXzTvbxZeH99c5UqUzKnfK9caha5wG0fxZyc1A07oSYg8rC4ldgJb0ezmTNb3YZYtuTuhF_FKj5C-pBOVcbNr0JRwAAuARRGyoPXHIrqIWiuWw_7AP0Vk4jqL-qQEKe-7DwxqMAm9DTr3ifBO7bVZp4o_u0RZC7gAkzyUghycblohrtC0xzMCBALW5rhSt9fbHBiQ4XLCEe5RA74fyUwRBev7x_QikwYgKmXrWWzqncXwleryWeT_yI5dFLegMOuQ5DJEIbx6g9YGEw3YXPsMza-6JQLviXW6Cc3q90rEeUzBUdBXbIj13_AN63VkguOjLzdOf1k2Rm8MwJCcA6H_WYcLPTsCTUAWVUYj1IJWv3HGqI2hSsvnlm3LYaO6rAuFZ0fJG1i1Cn2wG3FFaEEgMeGSuTrA-jD9kLZVOGFiWPGszJI90EGzJ37rTePxLcmBnI4QlVnVr7vpqv9wgtJ54i26gJmjAXVpsGbGBfv1anu8Tg2TzakoVPFWEpcz66HMSxdv5KzuUgjhtvJVXA4--3EwdFMA0xMCM9WlqVJpcUyuE0edw3K_Jsb18IBAB94i47Yxo2GNvS3ikS46jwdcmnxzZvbfJvSRgfVMO_zxdQAdimO3LJPzlKAJjfKXdBI0XvM6K4f5wK8AdCVXCS34iHp4zW5DeUL6Z-XZmQPaTuw3SLdnFw6Omqnan1NHlJLdVN_7Lnze2TZI-FQs683J1sSBaRFUonNPZbIRWHiahYj47d6gzxaSvhZ8mVzHWpqDyLtPXBnsXi13I_0zna5KrW-Ul0X0KlK7V9G56OK2TfWSLmtHy3dh7qFdNEY8SDSy7Nag";

        /// <summary>
        /// GET: /Cargar/UploadFile
        /// Muestra simplemente la vista de subida (que luego embebemos en CrearTicket.cshtml).
        /// </summary>
        public ActionResult UploadFile(
                int ticketId,
                int? userId,
                string returnController,
                string returnAction)
        {
            // Para que el POST tenga toda la info:
            ViewBag.TicketId = ticketId;
            ViewBag.UserId = userId;
            ViewBag.ReturnController = returnController;
            ViewBag.ReturnAction = returnAction;
            return View();
        }

        /// <summary>
        /// POST: /Cargar/UploadFile
        /// Recibe un archivo, lo sube a Dropbox y genera un enlace compartido.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(IFormFile file, int ticketId,
            int? userId,
            string returnController,
            string returnAction)
        {
            if (file == null || file.Length == 0)
            {
                TempData["UploadMessage"] = "No se seleccionó ningún archivo.";
                TempData["MessageType"] = "error";
                return RedirectToAction("CrearTicket", "CrearTicket");
            }

            try
            {
                using (var dbx = new DropboxClient(_dropboxApiToken))
                {
                    // Generamos la ruta dentro de Dropbox
                    var dropboxPath = $"/SupportlyTickets/{Path.GetFileName(file.FileName)}";

                    // Subimos el archivo
                    using (var stream = file.OpenReadStream())
                    {
                        var uploadResult = await dbx.Files.UploadAsync(
                            path: dropboxPath,
                            mode: WriteMode.Overwrite.Instance,
                            body: stream
                        );
                    }

                    // Creamos un enlace compartido (para vista directa)
                    var sharedLinkMetadata = await dbx.Sharing.CreateSharedLinkWithSettingsAsync(dropboxPath);

                    var dropboxLink = sharedLinkMetadata.Url.Replace("?dl=0", "?raw=1");

                    // Guardamos en TempData para que la vista CrearTicket los reciba
                    TempData["UploadMessage"] = "Archivo subido correctamente a Dropbox.";
                    TempData["MessageType"] = "success";
                    TempData["DropboxLink"] = dropboxLink;
                    TempData["UploadedFileName"] = Path.GetFileName(file.FileName);
                }
            }
            catch (Exception ex)
            {
                TempData["UploadMessage"] = $"Error al subir el archivo: {ex.Message}";
                TempData["MessageType"] = "error";
            }

            // Redirigimos a la acción GET CrearTicket para que la vista recargue y muestre el enlace
            return RedirectToAction(returnAction, returnController, new { id = ticketId, userId = userId });
        }
    }
}
