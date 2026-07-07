import BaseService from './baseService'

class EventTypeService extends BaseService {
  constructor() { super('/eventType') }
  getAll(params = {}) { return this.get('', { params }) }
  getById(id) { return this.get(`${id}`) }
  create(payload) { return this.post('', payload) }
  update(id, payload) { return this.put(`${id}`, payload) }
  remove(id) { return this.delete(`${id}`) }
}

export default new EventTypeService()
