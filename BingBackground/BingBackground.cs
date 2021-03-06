﻿using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BingBackground {
	class BingBackground {
		private const string BING_URL = "https://www.bing.com";
		
		private static void Main(string[] args) {
			if (!InternetConnection()) { ExitApp(1); return; }
			dynamic jsonObject = DownloadJson();
			string urlBase = GetBackgroundUrlBase(jsonObject);
			Image background = DownloadBackground(urlBase + GetResolutionExtension(urlBase));
			SaveBackground(background);
			SetBackground(PicturePosition.Fill);
		}
		/// <summary>
		/// Properly exits the application
		/// </summary>
		/// <param name="code">Return value used on exit</param>
		private static void ExitApp(byte code = 0) {
			if (System.Windows.Forms.Application.MessageLoop) {
				// WinForms app
				System.Windows.Forms.Application.Exit();
			} else {
				// Console app
				System.Environment.Exit(code);
			}
		}
		/// <summary>
		/// Checks if we have an internet connection
		/// </summary>
		/// <returns>Whether or not we have an internet connection</returns>
		private static bool InternetConnection() {
			try {
				using (WebClient client = new WebClient()) {
					using (Stream stream = client.OpenRead(BING_URL)) {
						return true;
					}
				}
			} catch {
				return false;
			}
		}
		/// <summary>
		/// Downloads the JSON data for the Bing Image of the Day
		/// </summary>
		/// <returns>JSON data for the Bing Image of the Day</returns>
		private static dynamic DownloadJson() {
			using (WebClient client = new WebClient()) {
				Console.WriteLine("Downloading JSON...");
				string jsonString = client.DownloadString(BING_URL + "/HPImageArchive.aspx?format=js&idx=0&n=1&mkt=" + GetConfigValue("CountryCode", "en-US"));
				return JsonConvert.DeserializeObject<dynamic>(jsonString);
			}
		}
		/// <summary>
		/// Gets the base URL for the Bing Image of the Day
		/// </summary>
		/// <returns>Base URL of the Bing Image of the Day</returns>
		private static string GetBackgroundUrlBase(dynamic jsonObject) {
			return BING_URL + jsonObject.images[0].urlbase;
		}
		/// <summary>
		/// Gets the title for the Bing Image of the Day
		/// </summary>
		/// <returns>Title of the Bing Image of the Day</returns>
		private static string GetBackgroundTitle(dynamic jsonObject) {
			string copyrightText = jsonObject.images[0].copyright;
			return copyrightText.Substring(0, copyrightText.IndexOf(" ("));
		}
		/// <summary>
		/// Checks to see if website at URL exists
		/// </summary>
		/// <param name="URL">The URL to check for existence</param>
		/// <returns>Whether or not website exists at URL</returns>
		private static bool WebsiteExists(string url) {
			try {
				WebRequest request = WebRequest.Create(url);
				request.Method = "HEAD";
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				return response.StatusCode == HttpStatusCode.OK;
			} catch {
				return false;
			}
		}
		/// <summary>
		/// Gets the resolution extension for the Bing Image of the Day URL
		/// </summary>
		/// <param name="URL">The base URL</param>
		/// <returns>The resolution extension for the URL</returns>
		private static string GetResolutionExtension(string url) {
			Rectangle resolution = Screen.PrimaryScreen.Bounds;
			string widthByHeight = GetConfigValue("ForceResolution", resolution.Width + "x" + resolution.Height);
			string potentialExtension = "_" + widthByHeight + ".jpg";
			if (WebsiteExists(url + potentialExtension)) {
				Console.WriteLine("Background for " + widthByHeight + " found.");
				return potentialExtension;
			} else {
				Console.WriteLine("No background for " + widthByHeight + " was found.");
				Console.WriteLine("Using 1920x1080 instead.");
				return "_1920x1080.jpg";
			}
		}
		/// <summary>
		/// Downloads the Bing Image of the Day
		/// </summary>
		/// <param name="URL">The URL of the Bing Image of the Day</param>
		/// <returns>The Bing Image of the Day</returns>
		private static Image DownloadBackground(string url) {
			Console.WriteLine("Downloading background...");
			WebRequest request = WebRequest.Create(url);
			WebResponse reponse = request.GetResponse();
			Stream stream = reponse.GetResponseStream();
			return Image.FromStream(stream);
		}
		/// <summary>
		/// Gets the path to My Pictures/Bing Backgrounds/yyyy/M-d-yyyy.jpg
		/// </summary>
		/// <returns>The path to My Pictures/Bing Backgrounds/yyyy/M-d-yyyy.jpg</returns>
		private static string GetBackgroundImagePath() {
			string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Bing Backgrounds", DateTime.Now.Year.ToString());
			Directory.CreateDirectory(directory);
			return Path.Combine(directory, DateTime.Now.ToString("M-d-yyyy") + ".jpg");
		}
		/// <summary>
		/// Saves the Bing Image of the Day to My Pictures/Bing Backgrounds/yyyy/M-d-yyyy.jpg
		/// <param name="background">The background image to save</param>
		private static void SaveBackground(Image background) {
			Console.WriteLine("Saving background...");
			background.Save(GetBackgroundImagePath(), System.Drawing.Imaging.ImageFormat.Jpeg);
		}
		/// <summary>
		/// Different types of PicturePositions to set backgrounds
		/// </summary>
		private enum PicturePosition {
			/// <summary>Tiles the picture on the screen</summary>
			Tile,
			/// <summary>Centers the picture on the screen</summary>
			Center,
			/// <summary>Stretches the picture to fit the screen</summary>
			Stretch,
			/// <summary>Fits the picture to the screen</summary>
			Fit,
			/// <summary>Crops the picture to fill the screen</summary>
			Fill
		}
		/// <summary>
		/// Methods that use platform invocation services
		/// </summary>
		internal sealed class NativeMethods {
			[DllImport("user32.dll", CharSet = CharSet.Auto)]
			internal static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
		}
		/// <summary>
		/// Sets the Bing Image of the Day as the desktop background
		/// </summary>
		/// <param name="style">The PicturePosition to use</param>
		private static void SetBackground(PicturePosition style) {
			Console.WriteLine("Setting background...");
			using (RegistryKey key = Registry.CurrentUser.OpenSubKey(Path.Combine("Control Panel", "Desktop"), true)) {
				switch (style) {
					case PicturePosition.Tile:
						key.SetValue("PicturePosition", "0");
						key.SetValue("TileWallpaper", "1");
						break;
					case PicturePosition.Center:
						key.SetValue("PicturePosition", "0");
						key.SetValue("TileWallpaper", "0");
						break;
					case PicturePosition.Stretch:
						key.SetValue("PicturePosition", "2");
						key.SetValue("TileWallpaper", "0");
						break;
					case PicturePosition.Fit:
						key.SetValue("PicturePosition", "6");
						key.SetValue("TileWallpaper", "0");
						break;
					case PicturePosition.Fill:
						key.SetValue("PicturePosition", "10");
						key.SetValue("TileWallpaper", "0");
						break;
				}
			}
			const int SetDesktopBackground = 20;
			const int UpdateIniFile = 1;
			const int SendWindowsIniChange = 2;
			NativeMethods.SystemParametersInfo(SetDesktopBackground, 0, GetBackgroundImagePath(), UpdateIniFile | SendWindowsIniChange);
		}
		/// <summary>
		/// Loads config variable from program's configuration file
		/// </summary>
		/// <param name="key">The key to load the value of</param>
		/// <param name="def">The default value if key not found or empty</param>
		/// <returns>Either value of the key on sucess or default value</returns>
		private static string GetConfigValue(string key, string def) {
			string val;
			return (String.IsNullOrEmpty(val = ConfigurationManager.AppSettings[key])) ? def : val;
		}
	}
}