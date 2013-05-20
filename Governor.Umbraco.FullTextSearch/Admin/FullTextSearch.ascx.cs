﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace FullTextSearch.Admin
{
    public partial class WebUserControl1 : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (msgmsg != null && Config.Instance.GetBooleanByKey("PublishEventRendering"))
            {
                msgmsg.InnerText = "Note: PublishEventRendering is active. This means every page on the site needs to be rendered when a button is pushed. So this will take a while. Just leave it to run.";
            }
        }

        protected void reCreate_Click(object sender, EventArgs e)
        {
            AdminActions.RebuildFullTextIndex();
            if (reCreate != null && msgCreate != null)
            {
                reIndex.Visible = false;
                reCreate.Visible = false;
                msgCreate.InnerText = "Index Re-Creation Triggered. ";
            }
        }
        protected void reIndex_Click(object sender, EventArgs e)
        {
            AdminActions.ReindexAllFullTextNodes();
            if (msgIndex != null && reIndex != null)
            {
                reIndex.Visible = false;
                reCreate.Visible = false;
                msgIndex.InnerText = "Re-Indexing Triggered.";
            }
        }
    }
}