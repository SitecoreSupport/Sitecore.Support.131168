namespace Sitecore.Support.Shell.Applications.Dialogs.GeneralLink
{
    using Sitecore;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.Resources.Media;
    using Sitecore.Shell.Applications.Dialogs;
   
    using Sitecore.Shell.Framework;
    using Sitecore.StringExtensions;
    using Sitecore.Utils;
    using Sitecore.Web;
    using Sitecore.Web.UI.HtmlControls;
    using Sitecore.Web.UI.Sheer;
    using Sitecore.Web.UI.WebControls;
    using Sitecore.Xml;
    using System;

    public class GeneralLinkForm : Sitecore.Shell.Applications.Dialogs.GeneralLink.GeneralLinkForm
    {
        private static void HideContainingRow(Control control)
        {
            Assert.ArgumentNotNull(control, "control");
            if (!Context.ClientPage.IsEvent)
            {
                GridPanel parent = control.Parent as GridPanel;
                if (parent != null)
                {
                    parent.SetExtensibleProperty(control, "row.style", "display:none");
                }
            }
            else
            {
                SheerResponse.SetStyle(control.ID + "Row", "display", "none");
            }
        }

        private void InitControls()
        {
            string str = string.Empty;
            string target = base.LinkAttributes["target"];
            string linkTargetValue = LinkForm.GetLinkTargetValue(target);
            if (linkTargetValue == "Custom")
            {
                str = target;
                this.CustomTarget.Disabled = false;
                this.Custom.Class = string.Empty;
            }
            else
            {
                this.CustomTarget.Disabled = true;
                this.Custom.Class = "disabled";
            }
            this.Text.Value = base.LinkAttributes["text"].Replace("&quot;", "\"");
            this.Target.Value = linkTargetValue;
            this.CustomTarget.Value = str;
            this.Class.Value = base.LinkAttributes["class"].Replace("&quot;", "\"");
            this.Querystring.Value = base.LinkAttributes["querystring"].Replace("&quot;", "\"");
            this.Title.Value = base.LinkAttributes["title"].Replace("&quot;", "\"");
            this.InitMediaLinkDataContext();
            this.InitInternalLinkDataContext();
        }

        private void InitInternalLinkDataContext()
        {
            this.InternalLinkDataContext.GetFromQueryString();
            string queryString = WebUtil.GetQueryString("ro");
            string str2 = base.LinkAttributes["id"];
            if (!string.IsNullOrEmpty(str2) && ID.IsID(str2))
            {
                ID itemID = new ID(str2);
                ItemUri uri = new ItemUri(itemID, Client.ContentDatabase);
                this.InternalLinkDataContext.SetFolder(uri);
            }
            if (queryString.Length > 0)
            {
                this.InternalLinkDataContext.Root = queryString;
            }
        }

        private void InitMediaLinkDataContext()
        {
            this.MediaLinkDataContext.GetFromQueryString();
            string id = base.LinkAttributes["url"];
            if (this.CurrentMode != "media")
            {
                id = string.Empty;
            }
            if (id.Length == 0)
            {
                id = "/sitecore/media library";
            }
            else
            {
                if (!id.StartsWith("/sitecore", StringComparison.InvariantCulture) && !id.StartsWith("/{11111111-1111-1111-1111-111111111111}", StringComparison.InvariantCulture))
                {
                    id = "/sitecore/media library" + id;
                }
                IDataView dataView = this.MediaLinkTreeview.GetDataView();
                if (dataView == null)
                {
                    return;
                }
                Item item = dataView.GetItem(id);
                if ((item != null) && (item.Parent != null))
                {
                    this.MediaLinkDataContext.SetFolder(item.Uri);
                }
            }
            this.MediaLinkDataContext.AddSelected(new DataUri(id));
            this.MediaLinkDataContext.Root = "/sitecore/media library";
        }



        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            if (!Context.ClientPage.IsEvent)
            {
                this.CurrentMode = base.LinkType ?? string.Empty;
                this.InitControls();
                this.SetModeSpecificControls();
                RegisterScripts();
            }
        }


        private static void RegisterScripts()
        {
            string script = "window.Texts = {{ ErrorOcurred: \"{0}\"}};".FormatWith(new object[] { Translate.Text("An error occured:") });
            Context.ClientPage.ClientScript.RegisterClientScriptBlock(Context.ClientPage.GetType(), "translationsScript", script, true);
        }

        private bool SetAnchorLinkAttributes(Packet packet)
        {
            Assert.ArgumentNotNull(packet, "packet");
            string str = this.LinkAnchor.Value;
            if ((str.Length > 0) && str.StartsWith("#", StringComparison.InvariantCulture))
            {
                str = str.Substring(1);
            }
            LinkForm.SetAttribute(packet, "url", str);
            LinkForm.SetAttribute(packet, "anchor", str);
            return true;
        }

        private void SetAnchorLinkControls()
        {
            ShowContainingRow(this.LinkAnchor);
            string str = base.LinkAttributes["anchor"];
            if ((base.LinkType != "anchor") && string.IsNullOrEmpty(this.LinkAnchor.Value))
            {
                str = string.Empty;
            }
            if (!string.IsNullOrEmpty(str) && !str.StartsWith("#", StringComparison.InvariantCulture))
            {
                str = "#" + str;
            }
            this.LinkAnchor.Value = str ?? string.Empty;
            this.SectionHeader.Text = Translate.Text("Specify the name of the anchor, e.g. #header1, and any additional properties");
        }

        private void SetCommonAttributes(Packet packet)
        {
            Assert.ArgumentNotNull(packet, "packet");
            LinkForm.SetAttribute(packet, "linktype", this.CurrentMode);
            LinkForm.SetAttribute(packet, "text", this.Text);
            LinkForm.SetAttribute(packet, "title", this.Title);
            LinkForm.SetAttribute(packet, "class", this.Class);
        }

        private bool SetExternalLinkAttributes(Packet packet)
        {
            Assert.ArgumentNotNull(packet, "packet");
            string str = this.Url.Value;
            if (((str.Length > 0) && (str.IndexOf("://", StringComparison.InvariantCulture) < 0)) && !str.StartsWith("/", StringComparison.InvariantCulture))
            {
                str = "http://" + str;
            }
            string linkTargetAttributeFromValue = LinkForm.GetLinkTargetAttributeFromValue(this.Target.Value, this.CustomTarget.Value);
            LinkForm.SetAttribute(packet, "url", str);
            LinkForm.SetAttribute(packet, "anchor", string.Empty);
            LinkForm.SetAttribute(packet, "target", linkTargetAttributeFromValue);
            return true;
        }

        private void SetExternalLinkControls()
        {
            if ((base.LinkType == "external") && string.IsNullOrEmpty(this.Url.Value))
            {
                string str = base.LinkAttributes["url"];
                this.Url.Value = str;
            }
            ShowContainingRow(this.UrlContainer);
            ShowContainingRow(this.Target);
            ShowContainingRow(this.CustomTarget);
            this.SectionHeader.Text = Translate.Text("Specify the URL, e.g. http://www.sitecore.net and any additional properties.");
        }

        private bool SetInternalLinkAttributes(Packet packet)
        {
            Assert.ArgumentNotNull(packet, "packet");
            Item selectionItem = this.InternalLinkTreeview.GetSelectionItem();
            if (selectionItem == null)
            {
                Context.ClientPage.ClientResponse.Alert("Select an item.");
                return false;
            }
            string linkTargetAttributeFromValue = LinkForm.GetLinkTargetAttributeFromValue(this.Target.Value, this.CustomTarget.Value);
            string str2 = this.Querystring.Value;
            if (str2.StartsWith("?", StringComparison.InvariantCulture))
            {
                str2 = str2.Substring(1);
            }
            LinkForm.SetAttribute(packet, "anchor", this.LinkAnchor);
            LinkForm.SetAttribute(packet, "querystring", str2);
            LinkForm.SetAttribute(packet, "target", linkTargetAttributeFromValue);
            LinkForm.SetAttribute(packet, "id", selectionItem.ID.ToString());
            return true;
        }

        private void SetInternalLinkContols()
        {
            this.LinkAnchor.Value = base.LinkAttributes["anchor"];
            this.InternalLinkTreeviewContainer.Visible = true;
            this.MediaLinkTreeviewContainer.Visible = false;
            ShowContainingRow(this.TreeviewContainer);
            ShowContainingRow(this.Querystring);
            ShowContainingRow(this.LinkAnchor);
            ShowContainingRow(this.Target);
            ShowContainingRow(this.CustomTarget);
            this.SectionHeader.Text = Translate.Text("Select the item that you want to create a link to and specify the appropriate properties.");
        }

        private bool SetJavascriptLinkAttributes(Packet packet)
        {
            Assert.ArgumentNotNull(packet, "packet");
            string str = this.JavascriptCode.Value;
            if ((str.Length > 0) && (str.IndexOf("javascript:", StringComparison.InvariantCulture) < 0))
            {
                str = "javascript:" + str;
            }
            LinkForm.SetAttribute(packet, "url", str);
            LinkForm.SetAttribute(packet, "anchor", string.Empty);
            return true;
        }

        private void SetJavaScriptLinkControls()
        {
            ShowContainingRow(this.JavascriptCode);
            string str = base.LinkAttributes["url"];
            if ((base.LinkType != "javascript") && string.IsNullOrEmpty(this.JavascriptCode.Value))
            {
                str = string.Empty;
            }
            this.JavascriptCode.Value = str;
            this.SectionHeader.Text = Translate.Text("Specify the JavaScript and any additional properties.");
        }

        private void SetMailLinkControls()
        {
            if ((base.LinkType == "mailto") && string.IsNullOrEmpty(this.Url.Value))
            {
                string str = base.LinkAttributes["url"];
                this.MailToLink.Value = str;
            }
            ShowContainingRow(this.MailToContainer);
            this.SectionHeader.Text = Translate.Text("Specify the email address and any additional properties. To send a test mail use the 'Send a test mail' button.");
        }

        private bool SetMailToLinkAttributes(Packet packet)
        {
            Assert.ArgumentNotNull(packet, "packet");
            string text = this.MailToLink.Value;
            text = StringUtil.GetLastPart(text, ':', text);
            if (!EmailUtility.IsValidEmailAddress(text))
            {
                SheerResponse.Alert("The e-mail address is invalid.", new string[0]);
                return false;
            }
            if (!string.IsNullOrEmpty(text))
            {
                text = "mailto:" + text;
            }
            LinkForm.SetAttribute(packet, "url", text ?? string.Empty);
            LinkForm.SetAttribute(packet, "anchor", string.Empty);
            return true;
        }

        private bool SetMediaLinkAttributes(Packet packet)
        {
            Assert.ArgumentNotNull(packet, "packet");
            Item selectionItem = this.MediaLinkTreeview.GetSelectionItem();
            if (selectionItem == null)
            {
                Context.ClientPage.ClientResponse.Alert("Select a media item.");
                return false;
            }
            string linkTargetAttributeFromValue = LinkForm.GetLinkTargetAttributeFromValue(this.Target.Value, this.CustomTarget.Value);
            LinkForm.SetAttribute(packet, "target", linkTargetAttributeFromValue);
            LinkForm.SetAttribute(packet, "id", selectionItem.ID.ToString());
            return true;
        }

        private void SetMediaLinkControls()
        {
            this.InternalLinkTreeviewContainer.Visible = false;
            this.MediaLinkTreeviewContainer.Visible = true;
            this.MediaPreview.Visible = true;
            this.UploadMedia.Visible = true;
            Item folder = this.MediaLinkDataContext.GetFolder();
            if (folder != null)
            {
                this.UpdateMediaPreview(folder);
            }
            ShowContainingRow(this.TreeviewContainer);
            ShowContainingRow(this.Target);
            ShowContainingRow(this.CustomTarget);
            this.SectionHeader.Text = Translate.Text("Select an item from the media library and specify any additional properties.");
        }

        private void SetModeSpecificControls()
        {
            HideContainingRow(this.TreeviewContainer);
            this.MediaPreview.Visible = false;
            this.UploadMedia.Visible = false;
            HideContainingRow(this.UrlContainer);
            HideContainingRow(this.Querystring);
            HideContainingRow(this.MailToContainer);
            HideContainingRow(this.LinkAnchor);
            HideContainingRow(this.JavascriptCode);
            HideContainingRow(this.Target);
            HideContainingRow(this.CustomTarget);
            switch (this.CurrentMode)
            {
                case "internal":
                    this.SetInternalLinkContols();
                    break;

                case "media":
                    this.SetMediaLinkControls();
                    break;

                case "external":
                    this.SetExternalLinkControls();
                    break;

                case "mailto":
                    this.SetMailLinkControls();
                    break;

                case "anchor":
                    this.SetAnchorLinkControls();
                    break;

                case "javascript":
                    this.SetJavaScriptLinkControls();
                    break;

                default:
                    throw new ArgumentException("Unsupported mode: " + this.CurrentMode);
            }
            foreach (Border border in this.Modes.Controls)
            {
                if (border != null)
                {
                    border.Class = (border.ID.ToLowerInvariant() == this.CurrentMode) ? "selected" : string.Empty;
                }
            }
        }

        private static void ShowContainingRow(Control control)
        {
            Assert.ArgumentNotNull(control, "control");
            if (!Context.ClientPage.IsEvent)
            {
                GridPanel parent = control.Parent as GridPanel;
                if (parent != null)
                {
                    parent.SetExtensibleProperty(control, "row.style", string.Empty);
                }
            }
            else
            {
                SheerResponse.SetStyle(control.ID + "Row", "display", string.Empty);
            }
        }

        private void UpdateMediaPreview(Item item)
        {
            Assert.ArgumentNotNull(item, "item");
            MediaUrlOptions thumbnailOptions = MediaUrlOptions.GetThumbnailOptions(item);
            thumbnailOptions.UseDefaultIcon = true;
            thumbnailOptions.Width = 0x60;
            thumbnailOptions.Height = 0x60;
            thumbnailOptions.Language = item.Language;
            thumbnailOptions.AllowStretch = false;
            string mediaUrl = MediaManager.GetMediaUrl(item, thumbnailOptions);
            this.MediaPreview.InnerHtml = "<img src=\"" + mediaUrl + "\" width=\"96px\" height=\"96px\" border=\"0\" alt=\"\" />";
        }


        private string CurrentMode
        {
            get
            {
                string str = base.ServerProperties["current_mode"] as string;
                if (!string.IsNullOrEmpty(str))
                {
                    return str;
                }
                return "internal";
            }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                base.ServerProperties["current_mode"] = value;
            }
        }
    }
}
