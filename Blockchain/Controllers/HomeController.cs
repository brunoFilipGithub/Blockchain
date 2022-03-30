using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Blockchain.Models;
using System.IO;
using Blockchain.Blockchain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Parameters;
using System.Text.Unicode;
using System.Text;
using Org.BouncyCastle.Utilities.Encoders;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Security;

namespace Blockchain.Controllers
{
    public class HomeController : Controller
    {
        public static int NUMBER_OF_AUTHORITY_USERS = 3;

        public HomeController()
        {

        }

        [HttpPost]
        public IActionResult AddVoteForPendingTransaction(string transactionHash)
        {
            List<LandTransaction> pendingTransactions = Helper.GetObjectListFromJson<LandTransaction>(ObjectType.PendingLandTransactions);

            LandTransaction landTransaction = pendingTransactions.Find(tr => tr.transactionHash == transactionHash);

            pendingTransactions.RemoveAll(tr => tr.transactionHash == transactionHash);
            
            bool success = Crypto.VerifySignature(landTransaction.transactionHash, landTransaction.digitalSignature, (RsaKeyParameters)PublicKeyFactory.CreateKey(landTransaction.publicKey));

            if (success == true)
            {
                landTransaction.votes += 1;
                landTransaction.authorityUsersThatVoted.Add(Crypto.ComputeSHA256(HttpContext.Session.GetLoggedUserHash()));

                if (((float)landTransaction.votes/(float)NUMBER_OF_AUTHORITY_USERS) < 0.5f)
                {
                    landTransaction.transactionStatus = Status.Voting;
                    pendingTransactions.Add(landTransaction);
                }
                else
                {
                    landTransaction.transactionStatus = Status.Verified;
                    Blockchain.Blockchain.AddBlock(landTransaction);
                }
            }

            Helper.WriteObjectListToJson<LandTransaction>(pendingTransactions, ObjectType.PendingLandTransactions);

            return Json("OK");
        }

        [HttpPost]
        public IActionResult SendLandToServer(LandModel land)
        {
            string loggedInUserHash = HttpContext.Session.GetLoggedUserHash();
            land.landRequesterHash = loggedInUserHash;

            land.points = Helper.NormalizeLandPoints(land.points);

            return Json(new { data = land, url = Url.Action("ViewLand", land) });
        }

        [HttpGet]
        public IActionResult ViewLand(LandModel land)
        {
            return View(land);
        }

        [HttpPost]
        public IActionResult SendRequest(LandModel land)
        {
            Land landType;
            Enum.TryParse(land.landId, out landType);

            LandTransaction landTransaction = new LandTransaction(land.landOwnerHash, land.landRequesterHash, land.location, landType);


            Helper.WriteNewPendingLandTransaction(landTransaction);
            return Json(new { data = land, url = Url.Action("ViewLand", land) });
        }

        [HttpPost]
        public IActionResult AcceptLandTransaction(string transactionHash)
        {
            var keyPair = Crypto.GenerateRandomKeyPair();
            string dataToSign = transactionHash;

            var signature = Crypto.SignData(dataToSign, (RsaKeyParameters)keyPair.Private);

            List<LandTransaction> pendingTransactions = Helper.GetObjectListFromJson<LandTransaction>(ObjectType.PendingLandTransactions);

            LandTransaction landTransaction = pendingTransactions.Find(element => element.transactionHash == transactionHash);

            pendingTransactions.RemoveAll(element => element.transactionHash == transactionHash);
            pendingTransactions.RemoveAll(element => element.landId == landTransaction.landId);

            landTransaction.digitalSignature = signature;

            SubjectPublicKeyInfo publicKeyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyPair.Public);
            byte[] serializedPublicBytes = publicKeyInfo.ToAsn1Object().GetDerEncoded();

            landTransaction.publicKey = serializedPublicBytes;

            RsaKeyParameters publicKey = (RsaKeyParameters) PublicKeyFactory.CreateKey(landTransaction.publicKey);

            landTransaction.transactionStatus = Status.Signed;

            pendingTransactions.Add(landTransaction);
            Helper.WriteObjectListToJson<LandTransaction>(pendingTransactions, ObjectType.PendingLandTransactions);

            return Json("OK");
        }

        [HttpPost]
        public IActionResult DeclineLandTransaction(string transactionHash)
        {
            List<LandTransaction> pendingTransactions = Helper.GetObjectListFromJson<LandTransaction>(ObjectType.PendingLandTransactions);

            pendingTransactions.RemoveAll(element => element.transactionHash == transactionHash);

            Helper.WriteObjectListToJson<LandTransaction>(pendingTransactions, ObjectType.PendingLandTransactions);

            return RedirectToAction("Index", "Home");
        }

        public IActionResult SignedTransactionsList()
        {
            List<LandTransaction> signedTransactions = Helper.GetSignedTransactions();
            List<LandTransaction> notVotedByThisAuthorityUser = new List<LandTransaction>();

            if (signedTransactions != null && signedTransactions.Count > 0)
            {
                foreach(LandTransaction landTransaction in signedTransactions)
                {
                    if(!landTransaction.authorityUsersThatVoted.Contains(Crypto.ComputeSHA256(HttpContext.Session.GetLoggedUserHash())))
                    {
                        notVotedByThisAuthorityUser.Add(landTransaction);
                    }
                }

                return View(notVotedByThisAuthorityUser);
            }
            else return RedirectToAction("Index", "Home");
        }

        public IActionResult RequestsList()
        {
            List<LandTransaction> requests = HttpContext.Session.GetRequestsForLoggedUser();

            if (requests != null && requests.Count > 0)
            {
                return View(requests);
            }
            else return RedirectToAction("Index", "Home");
        }

        // -----------------------------------------------------------------------------------------------------------------
        // ----------------------------------------- logins and registration -----------------------------------------------
        // -----------------------------------------------------------------------------------------------------------------

        [HttpGet]
        public IActionResult AuthorityLogin()
        {
            if (HttpContext.Session.IsUserLoggedIn() == false)
            {
                AuthorityLoginModel model = new AuthorityLoginModel();

                return View(model);
            }
            else return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult AuthorityLogin(AuthorityLoginModel model)
        {
            if (ModelState.IsValid)
            {
                string authorityLoginKey = model.AuthorityUserLoginKey;

                List<AuthorityUser> authorityUsers = Helper.GetObjectListFromJson<AuthorityUser>(ObjectType.AuthorityUser);

                AuthorityUser authorityUser;
                authorityUser = authorityUsers.FirstOrDefault(u => u.authorityUserLoginKeyHash == Crypto.ComputeSHA256(authorityLoginKey));

                if (authorityUser == null)
                {
                    return View(model);
                }
                else
                {
                    HttpContext.Session.SetObjectAsJson("loggedUser", authorityUser);

                    List<LandTransaction> signedTransactions = Helper.GetSignedTransactions();

                    if (signedTransactions != null)
                    {
                        HttpContext.Session.SetObjectAsJson("signedTransactions", signedTransactions);
                    }

                    return RedirectToAction("Index", "Home");
                }
            }
            else return View(model);
        }

        [HttpGet]
        public IActionResult UserRegister()
        {
            if (HttpContext.Session.IsUserLoggedIn() == false)
            {
                UserRegisterModel registerUser = new UserRegisterModel();
                return View(registerUser);
            }
            else return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult UserRegister(UserRegisterModel model)
        {
            List<User> users = Helper.GetObjectListFromJson<User>(ObjectType.User);

            foreach(User user in users)
            {
                if(user.OIB == model.OIB)
                {
                    ModelState.AddModelError("oib", "Korisnik s tim OIB-om je već registriran!");
                    break;
                }
            }

            if (ModelState.IsValid)
            {
                User user = new User() { OIB = model.OIB };

                Random random = new Random();
                int number = random.Next(150000, 15000000);

                Helper.SendKey(model.Email, user);

                return RedirectToAction("Index", "Home");
            }
            else return View(model);
        }

        [HttpGet]
        public IActionResult UserLogin()
        {
            if (HttpContext.Session.IsUserLoggedIn() == false)
            {
                UserLoginModel user = new UserLoginModel();

                return View(user);
            }
            else return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult UserLogin(UserLoginModel model)
        {
            if (ModelState.IsValid)
            {
                string loginKey = model.UserLoginKey;

                List<User> users = Helper.GetObjectListFromJson<User>(ObjectType.User);

                User user;
                user = users.FirstOrDefault(u => u.userLoginKeyHash == Crypto.ComputeSHA256(loginKey));

                if (user == null)
                {
                    return View(model);
                }
                else
                {
                    HttpContext.Session.SetObjectAsJson("loggedUser", user);
                    List<LandTransaction> requests = HttpContext.Session.GetRequestsForLoggedUser();

                    if (requests != null)
                    {
                        HttpContext.Session.SetObjectAsJson("userRequests", requests);
                    }

                    return RedirectToAction("Index", "Home");
                }
            }
            else return View(model);

        }

        public IActionResult LogOut()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetLatestLandDataJson()
        {
            return Json(Blockchain.Blockchain.GetLatestLandDataList());
        }

    }
}
