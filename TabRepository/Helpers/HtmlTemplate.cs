using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.Helpers
{
    public static class HtmlTemplate
    {
        public static string GetConfirmEmailHtml(string username, string callbackUrl)
        {
            return String.Format(@"
                <html style='font-family: sans-serif;
                            line-height: 1.15;
                            -webkit-text-size-adjust: 100%;
                            -ms-text-size-adjust: 100%;
                            -ms-overflow-style: scrollbar;
                            -webkit-tap-highlight-color: rgba(0, 0, 0, 0);'>
                    <head>
                        <style type='text/css'>
                            html {{
                                font-family: sans-serif;
                                line-height: 1.15;
                                -webkit-text-size-adjust: 100%;
                                -ms-text-size-adjust: 100%;
                                -ms-overflow-style: scrollbar;
                                -webkit-tap-highlight-color: rgba(0, 0, 0, 0);
                            }}
                            button {{
                                display: inline-block;
                                font-weight: 400;
                                text-align: center;
                                white-space: nowrap;
                                vertical-align: middle;
                                -webkit-user-select: none;
                                -moz-user-select: none;
                                -ms-user-select: none;
                                user-select: none;
                                border: 1px solid transparent;
                                padding: 0.375rem 0.75rem;
                                font-size: 1rem;
                                line-height: 1.5;
                                transition: color 0.15s ease-in-out, background-color 0.15s ease-in-out, border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out;
                                color: #ffffff !important;
                                background-color: rgb(134, 192, 144) !important;
                                border-color: rgb(134, 192, 144) !important;
                            }}
                            button:hover {{
                                cursor: pointer;
                            }}
                        </style>
                    </head>
                    <body>
                        <div style='max-width: 600px; background-color: black;'>
                            <table style='padding: 40px;'>
                                <tr>
                                    <td style='padding-bottom: 20px; text-align: center'>
                                        <img style='width: 150px' src='https://tabcollab.com/images/TabCollab_vertical_white.png'>
                                    </td>
                                </tr>
                                <tr style='width: 84%; min-height: 60%; background-color: rgb(24, 24, 24); color: white; margin-bottom: 20px; border: 1px solid silver'>
                                    <td style='border: 1px solid silver; margin: 30px 30px 10px 30px; padding: 40px;'>
                                        <p style='color: white;'>Hi {0}!</p>
                                        <br>
                                        <p style='line-height: 1.8; color: white;'>Thanks for creating your TabCollab account. To start using your account, please verify your email
                                            using the button below.</p>
                                        <br>
                                        <div style='text-align: center;/* padding-bottom: 40px; */'>
                                            <a href='{1}'>
                                                <button>Verify Email</button>
                                            </a>
                                        </div>
                                    </td>
                                </tr>
                            </table>
                        </div>
                    </body>
                </html>", username, callbackUrl);
        }

        public static string GetDynamicEmailHtml(string username, string message)
        {
            return String.Format(@"
                <html style='font-family: sans-serif;
                            line-height: 1.15;
                            -webkit-text-size-adjust: 100%;
                            -ms-text-size-adjust: 100%;
                            -ms-overflow-style: scrollbar;
                            -webkit-tap-highlight-color: rgba(0, 0, 0, 0);'>
                    <head>
                        <style type='text/css'>
                            html {{
                                font-family: sans-serif;
                                line-height: 1.15;
                                -webkit-text-size-adjust: 100%;
                                -ms-text-size-adjust: 100%;
                                -ms-overflow-style: scrollbar;
                                -webkit-tap-highlight-color: rgba(0, 0, 0, 0);
                            }}
                            button {{
                                display: inline-block;
                                font-weight: 400;
                                text-align: center;
                                white-space: nowrap;
                                vertical-align: middle;
                                -webkit-user-select: none;
                                -moz-user-select: none;
                                -ms-user-select: none;
                                user-select: none;
                                border: 1px solid transparent;
                                padding: 0.375rem 0.75rem;
                                font-size: 1rem;
                                line-height: 1.5;
                                transition: color 0.15s ease-in-out, background-color 0.15s ease-in-out, border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out;
                                color: #ffffff !important;
                                background-color: rgb(134, 192, 144) !important;
                                border-color: rgb(134, 192, 144) !important;
                            }}
                            button:hover {{
                                cursor: pointer;
                            }}
                        </style>
                    </head>
                    <body>
                        <div style='max-width: 600px; background-color: black;'>
                            <table style='padding: 40px;'>
                                <tr>
                                    <td style='padding-bottom: 20px; text-align: center'>
                                        <img style='width: 150px' src='https://tabcollab.com/images/TabCollab_vertical_white.png'>
                                    </td>
                                </tr>
                                <tr style='width: 84%; min-height: 60%; background-color: rgb(24, 24, 24); color: white; margin-bottom: 20px; border: 1px solid silver'>
                                    <td style='border: 1px solid silver; margin: 30px 30px 10px 30px; padding: 40px;'>
                                        <p style='color: white;'>Hi {0}!</p>
                                        <br>
                                        <p style='line-height: 1.8; color: white;'>{1}</p>
                                        <br>
                                    </td>
                                </tr>
                            </table>
                        </div>
                    </body>
                </html>", username, message);
        }
    }
}
