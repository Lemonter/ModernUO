using System.Text;
using Server.Accounting;
using Server.Mobiles;
using Server.Network;

namespace Server.Misc
{
    public static class Profile
    {
        public static void ChangeProfileRequest(Mobile beholder, Mobile beheld, string text)
        {
            if (beholder.ProfileLocked)
            {
                beholder.SendMessage("Твой профиль закрыт для изменений.");
            }
            else
            {
                beholder.Profile = text;
            }
        }

        public static void ProfileRequest(Mobile beholder, Mobile beheld)
        {
            if (!beheld.Player)
            {
                return;
            }

            if (beholder.Map != beheld.Map || !beholder.InRange(beheld, 12) || !beholder.CanSee(beheld))
            {
                return;
            }

            var header = Titles.ComputeTitle(beholder, beheld);

            var footer = "";

            if (beheld.ProfileLocked)
            {
                if (beholder == beheld)
                {
                    footer = "Your profile has been locked.";
                }
                else if (beholder.AccessLevel >= AccessLevel.Counselor)
                {
                    footer = "This profile has been locked.";
                }
            }

            if (footer.Length == 0 && beholder == beheld)
            {
                footer = GetAccountDuration(beheld);
            }

            // Заслуги идут в подвал, а не в тело. Тело — это то, что игрок сам о себе
            // написал, и клиент присылает его обратно целиком при любой правке: допиши туда
            // звания — и они осядут в тексте профиля, а на следующий показ допишутся ещё
            // раз. Подвал же только отображается.
            var earned = BuildEarned(beheld);

            if (earned.Length > 0)
            {
                footer = footer.Length == 0 ? earned : $"{earned}\n{footer}";
            }

            var body = beheld.Profile ?? "";
            var serial = beholder != beheld || !beheld.ProfileLocked ? beheld.Serial : Serial.Zero;

            beholder.NetState.SendDisplayProfile(serial, header, body, footer);
        }

        /// <summary>
        ///     Что человек заслужил: звание стражи и звания охотника.
        ///
        ///     Считается на лету из тех же систем, что дают за эти звания бонусы —
        ///     отдельного «списка достижений» нет намеренно, иначе он бы однажды разошёлся с
        ///     настоящим положением дел и врал бы игроку.
        ///
        ///     Видно всем, кто рядом, а не только владельцу: звание для того и нужно, чтобы
        ///     его видели.
        /// </summary>
        private static string BuildEarned(Mobile beheld)
        {
            var earned = new StringBuilder();

            if (beheld is PlayerMobile player)
            {
                var rank = Systems.MahaonGuard.GuardSystem.GetRankName(player);

                if (!string.IsNullOrEmpty(rank))
                {
                    earned.Append("Звание стражи: ").Append(rank).Append('\n');
                }
            }

            var titles = Systems.MahaonSlayer.MahaonSlayerTitles.TitlesOf(beheld);

            if (titles.Count > 0)
            {
                earned.Append("Звания охотника:\n");

                foreach (var title in titles)
                {
                    earned.Append("  ").Append(title).Append('\n');
                }
            }

            var progress = Systems.MahaonSlayer.MahaonSlayerTitles.ProgressOf(beheld);

            if (progress.Count > 0)
            {
                earned.Append("На счету:\n");

                foreach (var line in progress)
                {
                    earned.Append("  ").Append(line).Append('\n');
                }
            }

            return earned.ToString();
        }

        private static string GetAccountDuration(Mobile m)
        {
            if (m.Account is not Account a)
            {
                return "";
            }

            var age = a.AccountAge;

            if (Format(age.TotalDays, "This account is {0} day{1} old.", out var v))
            {
                return v;
            }

            if (Format(age.TotalHours, "This account is {0} hour{1} old.", out v))
            {
                return v;
            }

            if (Format(age.TotalMinutes, "This account is {0} minute{1} old.", out v))
            {
                return v;
            }

            if (Format(age.TotalSeconds, "This account is {0} second{1} old.", out v))
            {
                return v;
            }

            return "";
        }

        public static bool Format(double value, string format, out string op)
        {
            if (value >= 1.0)
            {
                op = string.Format(format, (int)value, (int)value != 1 ? "s" : "");
                return true;
            }

            op = null;
            return false;
        }
    }
}
