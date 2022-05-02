/* eslint-disable no-return-await */
const { Schema, model, connect } = require('mongoose')

let db = null

const UserSchema = new Schema(
  { userName: String },
  { timestamps: true }
)
const UserModel = model('User', UserSchema, 'Guestbook')

module.exports = {
  init: async function () {
    if (!db) {
      db = await connect(process.env['CosmosDbConnectionString'])
    }
  },
  addItem: async function (body) {
    const modelToInsert = new UserModel()
    modelToInsert['userName'] = body.name

    return modelToInsert.save()
  }
}
