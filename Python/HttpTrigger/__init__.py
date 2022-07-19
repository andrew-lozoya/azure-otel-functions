import os
import time
import logging
import requests
from opentelemetry import trace
from opentelemetry.instrumentation.requests import RequestsInstrumentor # This library allows auto tracing HTTP requests made by the requests library.
from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import OTLPSpanExporter
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor

import azure.functions as func

# Initialize tracing and an exporter that can send data to New Relic
provider = TracerProvider()
processor = BatchSpanProcessor(OTLPSpanExporter())
provider.add_span_processor(
    # BatchSpanProcessor buffers spans and sends them in batches in a
    # background thread. The default parameters are sensible, but can be
    # tweaked to optimize your performance
    processor
)
trace.set_tracer_provider(provider)
tracer = trace.get_tracer(__name__)

# Enable auto-instrumentation libraries
RequestsInstrumentor().instrument()

def main(req: func.HttpRequest) -> func.HttpResponse:
    # You can still use the OpenTelemetry API as usual to create spans if you are not using opentelemetry auto-instrumentation librarys
    # Start and activate a manual span indicating the HTTP request operation handling
    # in the server starts here
    logging.info("Python HTTP trigger function processed a request.")

    with tracer.start_as_current_span(
        "http-handler",
        kind=trace.SpanKind.SERVER,
        attributes={"http.status_code": 200, "http.status_text": "OK"},
    ):
        name = req.params.get("name")
        if not name:
            try:
                req_body = req.get_json()
            except ValueError as exc:
                # Record the exception and update the span status.
                span.record_exception(exc)
                span.set_status(trace.Status(trace.StatusCode.ERROR, str(exc)))
                pass
            else:
                name = req_body.get("name")

        if name:
            # Attach a new child and update the current span
            with tracer.start_as_current_span("doWork"):
                time.sleep(0.1)
                span = trace.get_current_span()
                span.set_attribute("user", name)

                # Auto-instrumentation for requests usage
                requests.post(
                    # Get the setting named 'myAppSetting'
                    url=f'{os.environ["URL"]}',
                    json={"name": f"{name}"},
                )

            # Close child span, set parent as current
            return func.HttpResponse(
                f"Hello, {name}. This HTTP triggered function executed successfully."
            )
        else:
            return func.HttpResponse(
                "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response.",
                status_code=200,
            )
    # Close parent span, set default span as current
