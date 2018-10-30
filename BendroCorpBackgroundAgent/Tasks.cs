using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using SendGrid;

namespace BendroCorpBackgroundAgent
{
    public class Tasks
    {
        static string generalDiscordChannel = Environment.GetEnvironmentVariable("WEBHOOK_LINK");
        static string discordMessagesChannel = Environment.GetEnvironmentVariable("DISCORD_MESSAGES");
        static string adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");

        public static bool hello(string MagicParam)
        {
            Console.WriteLine("Hello world! I am still working!");
            return true;
        }

        public static bool dead_users(string MagicParam)
        {
            try
            {
                PgUnicorn pg = new PgUnicorn();

                DataTable users = pg.DataTableOfSql("select * from users where id NOT IN (0, 1)");

                foreach (DataRow user in users.Rows)
                {
                    DateTime created = DateTime.Parse(user["created_at"].ToString());
                    if (created.AddDays(5) < DateTime.Now)
                    {
                        if (Convert.ToInt32(pg.ScalerOfSql("select count(*) from characters where user_id = " + user["id"].ToString())) == 0)
                        {
                            Console.WriteLine(user["username"] + " has no character and is more than 5 days old!");
                            // then the user should be culled
                            pg.ExecuteNonQueryOfSql(String.Format("delete from users where id = {0}", user["id"]));
                        }
                    }
                }

                //the op was completed
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Occured while trying to execute dead_users: " + ex.Message);
                return false;
            }
        }

        public static bool dormant_approvals(string MagicParam)
        {
            try
            {
                Console.WriteLine("Checking for dormant approvals...");
                PgUnicorn pg = new PgUnicorn();
                List<string> dormants = new List<string>();
                DataTable approvals = pg.DataTableOfSql(@"select app.id approval_id, toon.first_name, toon.last_name, us.email
                                                        from approval_approvers app,
                                                        characters toon,
                                                        users us
                                                        where app.approval_type_id < 4
                                                        and app.created_at <= NOW() + '1 day'::INTERVAL
                                                        and toon.user_id = app.user_id
                                                        and toon.is_main_character = true
                                                        and us.id = app.user_id");

                Console.WriteLine($"Found {approvals.Rows.Count} dormant approval(s)!");

                foreach (DataRow approval in approvals.Rows)
                {
                    // collects dormants
                    dormants.Add(String.Format("{0} {1} #{2}", approval["first_name"], approval["last_name"], approval["approval_id"]));
                    // send out reminder email
                    string emailMessage = $"<p>{approval["first_name"]},</p>"
                                         + "<p>You have a dormant approval that you need to approve or deny:</p>"
                                         + $"<p><a href=\"https://my.bendrocorp.com/requests/approvals/{approval["approval_id"]}\">Approval #{approval["approval_id"]}</a></p>"
                                         + "<p>Please correct this issue in a timely manner.</p>";
                    SendGridHelper sendGrid = new SendGridHelper(reciever: String.Format("{0} {1}", approval["first_name"], approval["last_name"]),
                                                                 email: approval["email"].ToString(),
                                                                 subject: "Dormant Approval",
                                                                 message: emailMessage
                                                                 );
                    sendGrid.Send().Wait();
                }
                // email admin with list
                if (dormants.Count > 0)
                {
                    string adminMessage = $"<p>Dormant approval check performed with {dormants.Count} results.</p>" +
                        $"<p>{string.Join("<br />", dormants)}</p>" +
                        "<p>Please harass the above individuals if they do finish their approvals in a timely manner.</p>";
                        
                        string DiscordAdminMessage = $"Dormant approval check performed with {dormants.Count} results: " +
                        $"{string.Join(", ", dormants)}. " +
                        "Please harass the above individuals if they do finish their approvals in a timely manner. ";
                    // email
                    SendGridHelper sendGrid = new SendGridHelper(reciever: adminEmail, email: adminEmail, subject: "Agents: Dormant Approvals Check", message: adminMessage);
                    sendGrid.Send().Wait();

                    //discord
                    WebhookSender.Send(discordMessagesChannel, new WebHookPayload { content = DiscordAdminMessage }).Wait();
                }

                Console.WriteLine("Finished checking for dormant approvals!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Occured while trying to execute dormant_approvals: " + ex.Message);
                return false;
            }
        }

        public static bool dormant_events(string magicParam)
        {
            try
            {
                Console.WriteLine("Checking for dormant events...");
                PgUnicorn unicorn = new PgUnicorn();
                DataTable dormantEvents = unicorn.DataTableOfSql("select * from events where end_date > NOW() + '24 hour'::INTERVAL and submitted_for_certification = false");

                Console.WriteLine($"{dormantEvents.Rows.Count} event(s) found!");

                List<string> dormants = new List<string>();
                foreach (DataRow item in dormantEvents.Rows)
                {
                    dormants.Add($"https://my.bendrocorp.com/events#!/{item["name"].ToString().ToLower().Replace(" ", "-")}-{item["id"]} - {item["name"]} #{item["id"]}");
                }

                string emailMessage = $"Dormant Events check performed with {dormantEvents.Rows.Count} results.\n"
                    + $"{string.Join("\n", dormants)}"
                    + "\nThis events need to be submitted for certification ASAP!";

                if (dormantEvents.Rows.Count > 0)
                {
                    SendGridHelper sendGrid = new SendGridHelper(reciever: "Boss Man",
                                                                     email: adminEmail,
                                                                     subject: "Dormant Events",
                                                                     message: emailMessage
                                                                     );
                    sendGrid.Send().Wait();
                    WebhookSender.Send(discordMessagesChannel, new WebHookPayload { content = emailMessage }).Wait();
                }

                Console.WriteLine("Finished checking for dormant events!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Occured while trying to execute dormant_events: " + ex.Message);
                return false;
            }
        }

        public static bool kaiden_discord_event_publish(string magicParam)
        {
            try
            {
                PgUnicorn unicorn = new PgUnicorn();
                DataTable eventsToAnnounce = unicorn.DataTableOfSql("select id, name from events where published_discord = false AND published = true");
                foreach (DataRow ev in eventsToAnnounce.Rows)
                {
                    // ex. https://my.bendrocorp.com/events#!/op-sunday-76
                    // Send WebHook to general chat
                    WebHookPayload webHookPayload = new WebHookPayload()
                    {
                        content = $"@everyone A new operation has been posted to the Employee Portal! You can get more information here: https://my.bendrocorp.com/events#!/{ev["name"].ToString().ToLower().Replace(" ", "-")}-{ev["id"].ToString()}"
                    };
                    WebhookSender.Send(generalDiscordChannel, webHookPayload).Wait();
                    unicorn.ExecuteNonQueryOfSql($"update events set published_discord = true where id = {ev["id"].ToString()}");
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
