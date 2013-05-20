﻿<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="FullTextSearch.ascx.cs" Inherits="FullTextSearch.Admin.WebUserControl1" %>
<p>This page allows you to trigger the re-indexing of the site. </p>
<p>Click here to re-index every node on the site</p>
<asp:Button ID="reIndex" runat="server" onclick="reIndex_Click" 
    Text="Re-Index" />
<p><label id="msgIndex" runat="server" /></p>
<p>Click here to delete the index and re-create from scratch. This can be useful if you're having problems.</p>
<asp:Button ID="reCreate" runat="server" onclick="reCreate_Click" 
    Text="Re-Create Index" />
<p><label id="msgCreate" runat="server" /></p>

<p><label id="msgmsg" runat="server" /></p>