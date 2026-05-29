import { describe, it, expect, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import ContactForm from '~/components/forms/ContactForm.vue'

describe('ContactForm', () => {
  it('валидирует имя (≥2), телефон и сообщение (≥10)', async () => {
    const onSubmit = vi.fn()
    const wrapper = mount(ContactForm, { props: { onSubmit } })

    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(onSubmit).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('Укажите имя')
    expect(wrapper.text()).toContain('Укажите телефон')
    expect(wrapper.text()).toContain('Сообщение слишком короткое')
  })

  it('отправляет валидный payload и показывает success', async () => {
    const onSubmit = vi.fn().mockResolvedValue({ message: 'Принято!' })
    const wrapper = mount(ContactForm, { props: { onSubmit } })

    const inputs = wrapper.findAll('input')
    await inputs[0].setValue('Иван')        // name
    await inputs[1].setValue('+79991234567') // phone
    await wrapper.find('textarea').setValue('Здравствуйте, нужен расчёт фасада дома.')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(onSubmit).toHaveBeenCalledWith({
      name: 'Иван',
      phone: '+79991234567',
      message: 'Здравствуйте, нужен расчёт фасада дома.',
    })
    expect(wrapper.text()).toContain('Принято!')
  })
})
