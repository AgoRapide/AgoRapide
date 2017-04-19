﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using AgoRapide;
using AgoRapide.Core;
using AgoRapide.Database;
using AgoRapide.API;

namespace AgoRapideSample {
    /// <summary>
    /// Contains various demonstrative methods.
    /// You may delete this controller from your own project.
    /// 
    /// This file also exists for the purpose of showing that you (of course) may have multiple Controllers in your project.
    /// Note how each controller should be added as a parameter in the call to 
    /// <see cref="APIMethod.CreateSemiAutogeneratedMethods"/> in <see cref="WebApiConfig.Register"/>
    /// </summary>
    public class AnotherController : BaseController {

        /// <summary>
        /// NOTICE TO DEVELOPER: Do not change any functionality here as method is used in the AgoRapide documentation 
        /// NOTICE TO DEVELOPER: (including demonstration of exception handling mechanism, like overflow for instance)
        /// </summary>
        /// <param name="SomeNumber"></param>
        /// <returns></returns>
        [HttpGet]
        [APIMethod(
            Description = "Doubles the number given (every time)",
            S1 = nameof(DemoDoubler), S2 = P.SomeNumber)]
        public object DemoDoubler(string SomeNumber) {
            try {
                if (!TryGetRequest(SomeNumber, out var request, out var completeErrorResponse)) return completeErrorResponse;
                var answer = checked(request.Parameters.PV<long>(P.SomeNumber.A()) * 2);
                return request.GetOKResponseAsText(answer.ToString(), "Your number doubled is: " + answer);
            } catch (Exception ex) {
                return HandleExceptionAndGenerateResponse(ex);
            } finally {
                DBDispose();
            }
        }

        [HttpGet]
        [APIMethod(
            Environment = AgoRapide.Environment.Development, // This method will not show up on your Production-server.
            Description = "Triples the number given (every time)",
            S1 = nameof(DemoTripler), S2 = P.SomeNumber)]
        public object DemoTripler(string SomeNumber) {
            try {
                if (!TryGetRequest(SomeNumber, out var request, out var completeErrorResponse)) return completeErrorResponse;
                var answer = request.Parameters.PV<long>(P.SomeNumber.A()) * 3;
                return request.GetOKResponseAsText(answer.ToString(), "Your number tripled is: " + answer);
            } catch (Exception ex) {
                return HandleExceptionAndGenerateResponse(ex);
            } finally {
                DBDispose();
            }
        }

        /// <summary>
        /// TODO: Implement 
        /// </summary>
        /// <param name="IntegerQueryId"></param>
        /// <returns></returns>
        [HttpGet]
        [APIMethod(
            Description = "Demonstrates use of " + nameof(PropertyKeyAttribute.IsMany) + ". Call with id of a Car-object",
            S1 = nameof(CarIsManyExample), S2 = CoreP.IntegerQueryId)]
        public object CarIsManyExample(string IntegerQueryId) {
            try {
                if (!TryGetRequest(IntegerQueryId, out var request, out var completeErrorResponse)) return completeErrorResponse;
                if (!DB.TryGetEntity(request.CurrentUser, request.Parameters.PVM<QueryIdInteger>(), AccessType.Read, useCache: false, entity: out Car car, errorResponse: out var errorResponse)) return request.GetErrorResponse(errorResponse);

                var v1 = "All colours (variant 1): " + string.Join(", ", car.PV<List<Colour>>(P.Colour2.A()).Select(c => c.ToString()));
                var v2 = "All colours (variant 2): " + car.PV<string>(P.Colour2.A());
                car.AddProperty(P.Colour2.A(), Colour.Blue);
                var v3 = "All colours (variant 3, Blue added): " + car.PV<string>(P.Colour2.A());
                // This will fail
                // car.AddProperty(P.Colour2.A(), new List<Colour> { Colour.Red, Colour.Green }); // Fails!                
                new List<Colour> { Colour.Red, Colour.Green }.ToIsManyParent(car, P.Colour2.A(), null).Use(
                    p => car.Properties[p.Key.Key.CoreP] = p); // This works
                var v4 = "All colours (variant 4, Only Red and Green): " + car.PV<string>(P.Colour2.A());

                // This of course works
                car.AddProperty(P.Colour3.A(), new List<Colour> { Colour.Red, Colour.Green });
                var v5 = "All colours (variant 5, colour3): " + car.PV<string>(P.Colour3.A());

                return request.GetOKResponseAsSingleEntity(car,
                    v1 + "\r\n" +
                    v2 + "\r\n" +
                    v3 + "\r\n" +
                    v4 + "\r\n" +
                    v5 + "\r\n");

            } catch (Exception ex) {
                return HandleExceptionAndGenerateResponse(ex);
            } finally {
                DBDispose();
            }
        }

    }
}