using System;
using System.Linq;
using System.Threading.Tasks;

namespace fiitobot.Services.Commands
{
    public class RandomCommandHandler : IChatCommandHandler
    {
        private readonly Random random;
        private readonly IRandomFeatureCommand[] randomCommands;

        public RandomCommandHandler(Random random, IRandomFeatureCommand[] randomCommands)
        {
            this.random = random;
            this.randomCommands = randomCommands;
        }

        public string Command => "/random";
        public ContactType[] AllowedFor => ContactTypes.AllNotExternal;

        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            var accWeight = 0;
            var weightsSum = randomCommands.Sum(command => command.Weight);
            var chance = random.NextDouble(0, weightsSum);
            // Пробегаемся по командам и смотрим, в какое "окно" вероятности попал шанс
            foreach (var command in randomCommands)
            {
                if (chance.InRange(accWeight, accWeight + command.Weight))
                {
                    await command.Execute(text, fromChatId, sender, silentOnNoResults);
                    return;
                }
                accWeight += command.Weight;
            }
        }
    }
}
