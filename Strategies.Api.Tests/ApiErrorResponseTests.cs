using Microsoft.AspNetCore.Mvc.ModelBinding;
using Strategies.Api.DTOs;
using Xunit;

namespace Strategies.Api.Tests
{
    public class ApiErrorResponseTests
    {
        [Fact]
        public void FromModelState_ListsEachMessageOnce()
        {
            var state = new ModelStateDictionary();
            state.AddModelError("Email", "A valid email address is required.");
            state.AddModelError("Password", "Password is required.");
            state.AddModelError("Other", "Password is required.");

            var body = ApiErrorResponse.FromModelState(state);

            Assert.Equal("Validation failed.", body.Message);
            Assert.Equal(new[] { "A valid email address is required.", "Password is required." }, body.Errors);
        }

        [Theory]
        [InlineData("$")]
        [InlineData("$.role")]
        [InlineData("")]
        public void FromModelState_UnreadableBody_GivesOnePlainMessage(string key)
        {
            var state = new ModelStateDictionary();
            state.AddModelError(key, "'b' is an invalid start of a property name. Path: $ | LineNumber: 0");
            state.AddModelError("request", "The request field is required.");

            var body = ApiErrorResponse.FromModelState(state);

            Assert.Equal(new[] { ApiErrorResponse.InvalidBodyMessage }, body.Errors);
        }

        [Fact]
        public void Serialised_WithoutCode_KeepsTheMessageAndErrorsShape()
        {
            var options = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);

            var plain = System.Text.Json.JsonSerializer.Serialize(new ApiErrorResponse { Message = "No." }, options);
            var coded = System.Text.Json.JsonSerializer.Serialize(
                new ApiErrorResponse { Message = "Taken.", Code = ApiErrorResponse.DuplicateEmailCode }, options);

            Assert.Equal("{\"message\":\"No.\",\"errors\":[]}", plain);
            Assert.Equal("{\"message\":\"Taken.\",\"errors\":[],\"code\":\"duplicate_email\"}", coded);
        }

        [Fact]
        public void FromModelState_ErrorWithoutMessage_GetsAReadableOne()
        {
            var state = new ModelStateDictionary();
            state.AddModelError("Email", new System.Exception(), new EmptyModelMetadataProvider().GetMetadataForType(typeof(object)));

            var body = ApiErrorResponse.FromModelState(state);

            Assert.Equal(new[] { ApiErrorResponse.InvalidBodyMessage }, body.Errors);
        }
    }
}
