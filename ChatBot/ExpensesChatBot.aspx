<%@ Page Language="C#" AutoEventWireup="true" CodeFile="ExpensesChatBot.aspx.cs" Inherits="ExpensesChatBot" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Expenses Chatbot</title>
    <style type="text/css">
        body {
            background: #f5f7fb;
            color: #1f2937;
            font-family: Arial, Helvetica, sans-serif;
            margin: 0;
        }

        .chatbot-shell {
            margin: 32px auto;
            max-width: 980px;
            padding: 0 16px;
        }

        .chatbot-card {
            background: #ffffff;
            border: 1px solid #d8dee9;
            border-radius: 12px;
            box-shadow: 0 10px 28px rgba(15, 23, 42, 0.08);
            overflow: hidden;
        }

        .chatbot-header {
            background: #0f5c9c;
            color: #ffffff;
            padding: 20px 24px;
        }

        .chatbot-header h1 {
            font-size: 24px;
            margin: 0 0 6px;
        }

        .chatbot-header p {
            margin: 0;
            opacity: 0.9;
        }

        .chatbot-body {
            padding: 24px;
        }

        .chatbot-message {
            background: #eef2f7;
            border-radius: 10px;
            margin-bottom: 18px;
            padding: 14px 16px;
            white-space: pre-line;
        }

        .chatbot-message.error {
            background: #fff2f2;
            border: 1px solid #f0b4b4;
            color: #9f1239;
        }

        .chatbot-examples {
            color: #4b5563;
            font-size: 14px;
            line-height: 1.5;
            margin: 0 0 18px;
        }

        .chatbot-thinking-note {
            background: #f8fafc;
            border: 1px solid #d8dee9;
            border-radius: 10px;
            color: #374151;
            margin-bottom: 18px;
            padding: 12px 14px;
        }

        .chatbot-input-row {
            align-items: flex-end;
            display: flex;
            gap: 10px;
            margin-bottom: 22px;
        }

        .chatbot-input-row label {
            display: block;
            font-weight: bold;
            margin-bottom: 6px;
        }

        .chatbot-input-wrap {
            flex: 1 1 auto;
        }

        .chatbot-input {
            border: 1px solid #c9d3df;
            border-radius: 8px;
            box-sizing: border-box;
            font-size: 15px;
            min-height: 42px;
            padding: 9px 12px;
            width: 100%;
        }

        .chatbot-send {
            background: #16a34a;
            border: 1px solid #15803d;
            border-radius: 8px;
            color: #ffffff;
            cursor: pointer;
            font-weight: bold;
            min-height: 42px;
            padding: 9px 18px;
        }

        .chatbot-results {
            border-collapse: collapse;
            width: 100%;
        }

        .chatbot-results th,
        .chatbot-results td {
            border: 1px solid #d8dee9;
            padding: 9px 10px;
            text-align: left;
            vertical-align: top;
        }

        .chatbot-results th {
            background: #eef2f7;
            font-weight: bold;
        }

        .chatbot-results tr:nth-child(even) td {
            background: #f8fafc;
        }

        @media (max-width: 640px) {
            .chatbot-input-row {
                align-items: stretch;
                flex-direction: column;
            }
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="chatbot-shell">
            <div class="chatbot-card">
                <div class="chatbot-header">
                    <h1>University Analytics Chatbot</h1>
                    <p>Ask a full Arabic or English question. The bot will classify, query SQL, think about the returned data, and explain the result.</p>
                </div>

                <div class="chatbot-body">
                    <div class="chatbot-thinking-note">
                        No buttons are needed. The chatbot decides whether your request is about revenues, expenses, files, invoices, people, fixed assets, or payments.
                    </div>

                    <div id="botMessage" runat="server" class="chatbot-message">
                        <strong><asp:Literal ID="litSelectedQuery" runat="server" Mode="Encode" /></strong><br />
                        <asp:Literal ID="litBotMessage" runat="server" Mode="Encode" />
                    </div>

                    <div class="chatbot-examples">
                        Examples: <strong>ما هي مجموع الايرادات الطلبة لكل الاعوام الدراسية؟</strong>,
                        <strong>show me expenses for Ahmed</strong>,
                        <strong>what is the expense code for invoice INV-10045</strong>,
                        <strong>find file links for receipt.pdf</strong>,
                        <strong>how many expenses for last 3 years</strong>,
                        <strong>اريد المصاريف الكلية للسنوات الثلاثة الاخيرة</strong>,
                        <strong>مجموع الموجودات الثابته مفصلة حسب الحسابات الثلاثير لمدة اخر 3 سنوات</strong>,
                        <strong>ما هي القيمة الكلية للمبالغ المصروفة الى حسين حيدر</strong>,
                        <strong>ابحث عن ملف رقم المستند 12345</strong>,
                        <strong>ابحث عن المستند رقم 42 لسنة 2024-2025</strong>,
                        <strong>اعرض الملفات بالتكلفة 1500</strong>.
                    </div>

                    <div class="chatbot-input-row">
                        <div class="chatbot-input-wrap">
                            <asp:Label ID="lblInput" runat="server" AssociatedControlID="txtUserInput" />
                            <asp:TextBox ID="txtUserInput" runat="server" CssClass="chatbot-input" MaxLength="200" />
                        </div>
                        <asp:Button ID="btnSend" runat="server" CssClass="chatbot-send" Text="Ask" OnClick="btnSend_Click" />
                    </div>

                    <asp:GridView ID="grdResults" runat="server"
                        AutoGenerateColumns="true"
                        CssClass="chatbot-results"
                        EmptyDataText="No results to show."
                        GridLines="None"
                        OnRowDataBound="grdResults_RowDataBound" />
                </div>
            </div>
        </div>
    </form>
</body>
</html>
