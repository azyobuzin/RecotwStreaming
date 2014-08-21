using System;
using Microsoft.AspNet.SignalR;

namespace RecotwStreaming
{
    public class MainHub : Hub
    {
        public async void Record(long id)
        {
            try
            {
                await RecotwReceiver.PushToAllInstanceAsync(
                    await RecotwApi.RecordTweetAsync(id).ConfigureAwait(false)
                ).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
