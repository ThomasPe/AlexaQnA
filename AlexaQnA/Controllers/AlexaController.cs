using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using Alexa.NET;
using QnaMakerApi;

namespace AlexaQnA.Controllers
{
    /// <summary>
    /// This is the main controller which will reveive 
    /// all requests from the Amazon Alexa service
    /// and provide the necessary response
    /// </summary>
    /// <remarks>
    /// URI will look like this: https://mysite.azurewebsites.net/api/Alexa
    /// </remarks>
    [Produces("application/json")]
    [Route("api/Alexa")]
    public class AlexaController : Controller
    {
        private IConfiguration _config;
        private string _appid;

        /// <summary>
        /// Constructor will get the Configuration injected
        /// and gets the app id from the appsettings.json
        /// </summary>
        /// <param name="config"></param>
        public AlexaController(IConfiguration config)
        {
            _config = config;
            _appid = _config.GetValue<string>("SkillApplicationId");
        }

        /// <summary>
        /// This method will receive the acutal post request from Amazon
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult HandleSkillRequest([FromBody]SkillRequest input)
        {
            // Security check
            // Only accept requests with known app id
            if (input.Session.Application.ApplicationId != _appid)
            {
                return BadRequest();
            }

            var requestType = input.GetRequestType();
            if (requestType == typeof(IntentRequest))
            {
                var response = HandleIntents(input);
                return Ok(response);
            }
            else if (requestType == typeof(LaunchRequest))
            {
                var speech = new SsmlOutputSpeech
                {
                    Ssml = "<speak>Launch repsonse</speak>"
                };
                var finalResponse = ResponseBuilder.Tell(speech);
                return Ok(finalResponse);
            }
            else if (requestType == typeof(AudioPlayerRequest))
            {
                var speech = new SsmlOutputSpeech
                {
                    Ssml = "<speak>Audio player repsonse</speak>"
                };
                var finalResponse = ResponseBuilder.Tell(speech);
                return Ok(finalResponse);
            }
            return Ok(ErrorResponse());
        }

        /// <summary>
        /// This method will handle the different intents
        /// </summary>
        /// <param name="input"></param>
        /// <returns>Ready-to-send SkillResponse</returns>
        private async Task<SkillResponse> HandleIntents(SkillRequest input)
        {
            var intentRequest = input.Request as IntentRequest;
            var speech = new SsmlOutputSpeech();

            // check the name to determine what you should do
            if (intentRequest.Intent.Name.Equals("CatchAllIntent"))
            {
                var phrase = intentRequest.Intent.Slots["phrase"].Value;
                string answer = string.Empty;

                // get an answer from QnA Maker
                using (var client = new QnaMakerClient(_config.GetValue<string>("QnAMakerSubscriptionKey")))
                {
                    var result = await client.GenerateAnswer(new Guid(_config.GetValue<string>("QnAMakerKnowledgeBaseId")), phrase);
                    answer = result.Answers[0].Answer;
                }

                speech.Ssml = $"<speak>{answer}</speak>";
                var response = ResponseBuilder.Tell(speech);
                return response;
            }
            else
            {
                return ErrorResponse();
            }
        }

        /// <summary>
        /// A default error message for all unexpected issues
        /// </summary>
        /// <returns>Error Message</returns>
        private SkillResponse ErrorResponse()
        {
            var speech = new SsmlOutputSpeech
            {
                Ssml = "<speak>I'm sorry, something went wrong.</speak>"
            };
            return ResponseBuilder.Tell(speech);
        }
    }
}