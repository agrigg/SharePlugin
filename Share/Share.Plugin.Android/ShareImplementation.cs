using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Support.CustomTabs;
using Plugin.CurrentActivity;
using Plugin.Share.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms.Platform.Android;

namespace Plugin.Share
{
    /// <summary>
    /// Implementation for Feature
    /// </summary>
    public class ShareImplementation : IShare
    {
        /// <summary>
        /// Open a browser to a specific url
        /// </summary>
        /// <param name="url">Url to open</param>
        /// <param name="options">Platform specific options</param>
        /// <returns>True if the operation was successful, false otherwise</returns>
        public Task<bool> OpenBrowser(string url, BrowserOptions options = null)
        {
            try
            {
                if (options == null)
                    options = new BrowserOptions();

                if (CrossCurrentActivity.Current.Activity == null)
                {
                    var intent = new Intent(Intent.ActionView);
                    intent.SetData(Android.Net.Uri.Parse(url));

                    intent.SetFlags(ActivityFlags.ClearTop);
                    intent.SetFlags(ActivityFlags.NewTask);
                    Application.Context.StartActivity(intent);
                }
                else
                {
                    var tabsBuilder = new CustomTabsIntent.Builder();
                    tabsBuilder.SetShowTitle(options?.ChromeShowTitle ?? false);

                    var toolbarColor = options?.ChromeToolbarColor;
                    if (toolbarColor != null)
                        tabsBuilder.SetToolbarColor(toolbarColor.ToNativeColor());

                    var intent = tabsBuilder.Build();
                    intent.LaunchUrl(CrossCurrentActivity.Current.Activity, Android.Net.Uri.Parse(url));
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to open browser: " + ex.Message);
                return Task.FromResult(false);
            }
        }


        /// <summary>
        /// Share a message with compatible services
        /// </summary>
        /// <param name="message">Message to share</param>
        /// <param name="options">Platform specific options</param>
        /// <returns>True if the operation was successful, false otherwise</returns>
        public async Task<bool> Share(ShareMessage message, ShareOptions options = null)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            try
            {
                var items = new List<string>();
                if (message.Text != null)
                    items.Add(message.Text);
                if (message.Url != null)
                    items.Add(message.Url);

                var intent = new Intent(Intent.ActionSend);

                intent.PutExtra(Intent.ExtraText, string.Join(Environment.NewLine, items));

                if (message.Title != null)
                    intent.PutExtra(Intent.ExtraSubject, message.Title);

                if(message.Image != null){
                    var handler = new ImageLoaderSourceHandler();
                    var bitmap = await handler.LoadImageAsync(message.Image, Android.App.Application.Context);

                    var path = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads
                                                                                        + Java.IO.File.Separator + Guid.NewGuid().ToString() + ".png");

                    using (var os = new System.IO.FileStream(path.AbsolutePath, System.IO.FileMode.Create))
                    {
                        bitmap.Compress(Bitmap.CompressFormat.Png, 100, os);
                    }

					intent.SetType("image/*");
					intent.SetFlags(ActivityFlags.GrantReadUriPermission);
                    intent.PutExtra(Intent.ExtraStream, Android.Net.Uri.FromFile(path));
                }
                else {
                    intent.SetType("text/plain");
                }

                var chooserIntent = Intent.CreateChooser(intent, options?.ChooserTitle);
                chooserIntent.SetFlags(ActivityFlags.ClearTop);
                chooserIntent.SetFlags(ActivityFlags.NewTask);
                Application.Context.StartActivity(chooserIntent);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to share: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Sets text of the clipboard
        /// </summary>
        /// <param name="text">Text to set</param>
        /// <param name="label">Label to display (not required, Android only)</param>
        /// <returns>True if the operation was successful, false otherwise</returns>
        public Task<bool> SetClipboardText(string text, string label = null)
        {
            try
            {
                var sdk = (int)Android.OS.Build.VERSION.SdkInt;
                if (sdk < (int)Android.OS.BuildVersionCodes.Honeycomb)
                {
                    var clipboard = (Android.Text.ClipboardManager)Application.Context.GetSystemService(Context.ClipboardService);
                    clipboard.Text = text;
                }
                else
                {
                    var clipboard = (ClipboardManager)Application.Context.GetSystemService(Context.ClipboardService);
                    clipboard.PrimaryClip = ClipData.NewPlainText(label ?? string.Empty, text);
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to copy to clipboard: " + ex.Message);
                return Task.FromResult(false);
            }
        }

		/// <summary>
		/// Checks if the url can be opened
		/// </summary>
		/// <param name="url">Url to check</param>
		/// <returns>True if it can</returns>
		public bool CanOpenUrl(string url)
		{
			try
			{
				var context = CrossCurrentActivity.Current.Activity ?? Application.Context;
				var intent = new Intent(Intent.ActionView);
				intent.SetData(Android.Net.Uri.Parse(url));

				intent.SetFlags(ActivityFlags.ClearTop);
				intent.SetFlags(ActivityFlags.NewTask);
				return intent.ResolveActivity(context.PackageManager) != null;
			}
			catch (Exception ex)
			{
				return false;
			}
		}

		/// <summary>
		/// Gets if cliboard is supported
		/// </summary>
		public bool SupportsClipboard => true;
    }
}