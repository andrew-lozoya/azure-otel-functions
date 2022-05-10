const { v4: uuidv4 } = require('uuid')
const otel = require('@opentelemetry/api')
const grpc = require('@grpc/grpc-js')
const { ConsoleSpanExporter, SimpleSpanProcessor } = require('@opentelemetry/sdk-trace-base')
const { MongooseInstrumentation } = require('opentelemetry-instrumentation-mongoose')
const { NodeTracerProvider } = require('@opentelemetry/sdk-trace-node')
const { registerInstrumentations } = require('@opentelemetry/instrumentation')
const { SemanticResourceAttributes } = require('@opentelemetry/semantic-conventions')
const { OTLPTraceExporter } = require('@opentelemetry/exporter-trace-otlp-grpc')
const { Resource } = require('@opentelemetry/resources')

// enable logging ONLY for developement
otel.diag.setLogger(
  new otel.DiagConsoleLogger(),
  otel.DiagLogLevel.DEBUG
)

module.exports = () => {
  const resources = new Resource({
    [SemanticResourceAttributes.SERVICE_NAME]: 'OpenTelemetry-Node.JS-Example',
    [SemanticResourceAttributes.SERVICE_INSTANCE_ID]: uuidv4()
  })

  // Create and configure NodeTracerProvider
  const provider = new NodeTracerProvider({
    resource: resources
  })

  // Add NR OTLP endpoint and insert key
  const url = "grpc://staging.otlp.nr-data.net:4317"
  const metadata = new grpc.Metadata()
  // we're assuming that the correct insert key is set as a function environment variable.
  metadata.set("api-key", process.env.NEW_RELIC_INSERT_KEY)

  // New Relic requires TLS.
  const credentials = grpc.credentials.createSsl()
  const collectorOptions = {
    credentials,
    metadata,
    url
  }

  const traceExporter = new OTLPTraceExporter(collectorOptions)

  // Configure span processor to send spans to the traceExporter
  provider.addSpanProcessor(new SimpleSpanProcessor(traceExporter))
  provider.addSpanProcessor(new SimpleSpanProcessor(new ConsoleSpanExporter()))
  provider.register()

  // register and load instrumentation
  // but instrumentations needs to be added
  registerInstrumentations({
    instrumentations: [
    // provides automatic instrumentation for Mongoose.
      new MongooseInstrumentation({
      // see under for available configuration
        dbStatementSerializer: (_operation, payload) => {
          return JSON.stringify(payload)
        }
      })
    ]
  })
  // This is what we'll access in all instrumentation code
  return otel.trace.getTracer('instrumentation-example', '1.2.0')
}
