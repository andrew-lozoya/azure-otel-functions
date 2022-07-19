const tracer = require('./tracer')()
const otel = require('@opentelemetry/api')
const mongoUtil = require('../lib/azure-cosmosdb-mongodb')

module.exports = async function (context, req) {
  // Context Propagation
  remoteCtx = otel.propagation.extract(otel.ROOT_CONTEXT, req.headers);
  const parentSpan = tracer.startSpan('handleRequest', {
    kind: otel.SpanKind.CONSUMER,
    attributes: {
      'invocationId': context.invocationId,
      'http.method': req.method
    }
  }, remoteCtx)

  const name = (req.query.name || (req.body && req.body.name))
  const responseMessage = name ?
    'Hello, ' + name + '. This HTTP triggered function executed successfully.' :
    'This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response.'
  // Set attributes to the span.
  parentSpan.setAttribute('user', name)

  // Start another span. In this example, the async function method already started a
  // parentSpan, so that will be the parent span, and this will be a child span.
  const ctx = otel.trace.setSpan(otel.context.active(), parentSpan)
  const childspan = tracer.startSpan('doWork', undefined, ctx)

  // Simulate some random work.
  for (let i = 0; i <= Math.floor(Math.random() * 40000000); i += 1) {
    // empty
  }
  // Annotate our span to capture metadata about our operation.
  childspan.addEvent('Invoking doWork', {
    'invocationId': context.invocationId
  })

  // IMPORTANT! Auto-instrumented modules currently do not gather context automatically
  // Use ROOT_CONTEXT to pass the current context into this call tree
  otel.context.with(otel.trace.setSpan(otel.ROOT_CONTEXT, childspan), async () => {
    await mongoUtil.init()
    await mongoUtil.addItem(req.body)
  })

  childspan.end()
  parentSpan.end()

  context.res = {
    // status: 200, /* Defaults to 200 */
    body: responseMessage
  }
}