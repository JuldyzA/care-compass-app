using System.Text.Encodings.Web;

namespace TeamYellow.Services
{
    /// <summary>
    /// Service that provides styled HTML email templates for user communications.
    /// </summary>
    public class EmailTemplateService
    {
        /// <summary>
        /// Generates a styled HTML email for email confirmation during registration.
        /// </summary>
        /// <param name="confirmationUrl">The URL for confirming the email address.</param>
        /// <returns>HTML formatted email body.</returns>
        public static string GenerateRegistrationConfirmationEmail(string confirmationUrl)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <style>
                        body {{
                            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
                            line-height: 1.6;
                            color: #333;
                            margin: 0;
                            padding: 0;
                            background-color: #f5f5f5;
                        }}
                        .email-container {{
                            max-width: 600px;
                            margin: 0 auto;
                            background-color: #ffffff;
                            border-radius: 8px;
                            overflow: hidden;
                            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
                        }}
                        .email-header {{
                            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                            color: #ffffff;
                            padding: 40px 20px;
                            text-align: center;
                        }}
                        .email-header h1 {{
                            margin: 0;
                            font-size: 28px;
                            font-weight: 600;
                        }}
                        .email-body {{
                            padding: 40px 20px;
                        }}
                        .email-section {{
                            margin-bottom: 30px;
                        }}
                        .email-section h2 {{
                            font-size: 20px;
                            color: #333;
                            margin-top: 0;
                            margin-bottom: 15px;
                        }}
                        .email-section p {{
                            margin: 10px 0;
                            color: #555;
                            font-size: 16px;
                        }}
                        .cta-button {{
                            display: inline-block;
                            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                            color: #ffffff;
                            padding: 12px 32px;
                            text-decoration: none;
                            border-radius: 6px;
                            font-weight: 600;
                            font-size: 16px;
                            margin: 20px 0;
                            text-align: center;
                            transition: all 0.3s ease;
                        }}
                        .cta-button:hover {{
                            opacity: 0.9;
                        }}
                        .email-footer {{
                            background-color: #f9f9f9;
                            padding: 20px;
                            text-align: center;
                            border-top: 1px solid #e0e0e0;
                        }}
                        .email-footer p {{
                            margin: 5px 0;
                            font-size: 14px;
                            color: #999;
                        }}
                        .security-note {{
                            background-color: #f0f7ff;
                            border-left: 4px solid #667eea;
                            padding: 15px;
                            margin: 20px 0;
                            border-radius: 4px;
                        }}
                        .security-note p {{
                            margin: 0;
                            color: #333;
                            font-size: 14px;
                        }}
                        .highlight {{
                            color: #667eea;
                            font-weight: 600;
                        }}
                    </style>
                </head>
                <body>
                    <div class='email-container'>
                        <div class='email-header'>
                            <h1>Welcome to CareCompass</h1>
                        </div>
                        <div class='email-body'>
                            <div class='email-section'>
                                <h2>Confirm Your Email Address</h2>
                                <p>Thank you for creating a CareCompass account! We're excited to have you on board.</p>
                                <p>To complete your registration and activate your account, please confirm your email address by clicking the button below.</p>
                            </div>
                            <div style='text-align: center;'>
                                <a href='{confirmationUrl}' class='cta-button'>Confirm Email Address</a>
                            </div>
                            <div class='email-section'>
                                <p><strong>Or copy and paste this link in your browser:</strong></p>
                                <p style='word-break: break-all; font-size: 13px; color: #666; background-color: #f5f5f5; padding: 10px; border-radius: 4px;'>{HtmlEncoder.Default.Encode(confirmationUrl)}</p>
                            </div>
                            <div class='security-note'>
                                <p><strong>Security Reminder:</strong> This link will expire in 24 hours. If you didn't create this account, you can safely ignore this email.</p>
                            </div>
                            <div class='email-section'>
                                <p>If you have any questions or need assistance, don't hesitate to reach out to our support team.</p>
                                <p>Best regards,<br><span class='highlight'>The CareCompass Team</span></p>
                            </div>
                        </div>
                        <div class='email-footer'>
                            <p>© 2026 CareCompass. All rights reserved.</p>
                            <p>This is an automated message. Please do not reply to this email.</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        /// <summary>
        /// Generates a styled HTML email for confirming an email address change.
        /// </summary>
        /// <param name="confirmationUrl">The URL for confirming the new email address.</param>
        /// <returns>HTML formatted email body.</returns>
        public static string GenerateEmailChangeConfirmationEmail(string confirmationUrl)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <style>
                        body {{
                            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
                            line-height: 1.6;
                            color: #333;
                            margin: 0;
                            padding: 0;
                            background-color: #f5f5f5;
                        }}
                        .email-container {{
                            max-width: 600px;
                            margin: 0 auto;
                            background-color: #ffffff;
                            border-radius: 8px;
                            overflow: hidden;
                            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
                        }}
                        .email-header {{
                            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                            color: #ffffff;
                            padding: 40px 20px;
                            text-align: center;
                        }}
                        .email-header h1 {{
                            margin: 0;
                            font-size: 28px;
                            font-weight: 600;
                        }}
                        .email-body {{
                            padding: 40px 20px;
                        }}
                        .email-section {{
                            margin-bottom: 30px;
                        }}
                        .email-section h2 {{
                            font-size: 20px;
                            color: #333;
                            margin-top: 0;
                            margin-bottom: 15px;
                        }}
                        .email-section p {{
                            margin: 10px 0;
                            color: #555;
                            font-size: 16px;
                        }}
                        .cta-button {{
                            display: inline-block;
                            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                            color: #ffffff;
                            padding: 12px 32px;
                            text-decoration: none;
                            border-radius: 6px;
                            font-weight: 600;
                            font-size: 16px;
                            margin: 20px 0;
                            text-align: center;
                            transition: all 0.3s ease;
                        }}
                        .cta-button:hover {{
                            opacity: 0.9;
                        }}
                        .email-footer {{
                            background-color: #f9f9f9;
                            padding: 20px;
                            text-align: center;
                            border-top: 1px solid #e0e0e0;
                        }}
                        .email-footer p {{
                            margin: 5px 0;
                            font-size: 14px;
                            color: #999;
                        }}
                        .security-note {{
                            background-color: #fffacd;
                            border-left: 4px solid #ffc107;
                            padding: 15px;
                            margin: 20px 0;
                            border-radius: 4px;
                        }}
                        .security-note p {{
                            margin: 0;
                            color: #333;
                            font-size: 14px;
                        }}
                        .highlight {{
                            color: #667eea;
                            font-weight: 600;
                        }}
                        .email-info {{
                            background-color: #f5f5f5;
                            padding: 15px;
                            border-radius: 4px;
                            margin: 15px 0;
                        }}
                    </style>
                </head>
                <body>
                    <div class='email-container'>
                        <div class='email-header'>
                            <h1>Email Address Change Request</h1>
                        </div>
                        <div class='email-body'>
                            <div class='email-section'>
                                <h2>Confirm Your New Email Address</h2>
                                <p>We received a request to change the email address associated with your CareCompass account.</p>
                                <p>To confirm this change and update your account, please click the button below.</p>
                            </div>
                            <div style='text-align: center;'>
                                <a href='{confirmationUrl}' class='cta-button'>Confirm Email Change</a>
                            </div>
                            <div class='email-section'>
                                <p><strong>Or copy and paste this link in your browser:</strong></p>
                                <p style='word-break: break-all; font-size: 13px; color: #666; background-color: #f5f5f5; padding: 10px; border-radius: 4px;'>{HtmlEncoder.Default.Encode(confirmationUrl)}</p>
                            </div>
                            <div class='security-note'>
                                <p><strong>⚠️ Security Alert:</strong> This link will expire in 24 hours. If you did not request this change, please ignore this email or contact our support team immediately.</p>
                            </div>
                            <div class='email-info'>
                                <p><strong>What happens next:</strong> Once confirmed, your account email will be updated. You'll then use the new email address to log in to CareCompass.</p>
                            </div>
                            <div class='email-section'>
                                <p>If you have any questions or concerns about this request, please contact our support team.</p>
                                <p>Best regards,<br><span class='highlight'>The CareCompass Team</span></p>
                            </div>
                        </div>
                        <div class='email-footer'>
                            <p>© 2026 CareCompass. All rights reserved.</p>
                            <p>This is an automated message. Please do not reply to this email.</p>
                        </div>
                    </div>
                </body>
                </html>";
        }
    }
}