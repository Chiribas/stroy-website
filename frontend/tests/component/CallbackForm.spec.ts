import { describe, it, expect, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import CallbackForm from '~/components/forms/CallbackForm.vue'

describe('CallbackForm', () => {
  it('не вызывает onSubmit при пустом телефоне и показывает ошибку', async () => {
    const onSubmit = vi.fn()
    const wrapper = mount(CallbackForm, { props: { onSubmit } })

    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(onSubmit).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('Укажите телефон')
  })

  it('вызывает onSubmit с телефоном и показывает success-сообщение', async () => {
    const onSubmit = vi.fn().mockResolvedValue({ message: 'Спасибо!' })
    const wrapper = mount(CallbackForm, { props: { onSubmit } })

    await wrapper.find('input[type="tel"]').setValue('+79991234567')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(onSubmit).toHaveBeenCalledWith({ phone: '+79991234567', name: undefined })
    expect(wrapper.text()).toContain('Спасибо!')
  })

  it('показывает ошибку, если onSubmit бросает', async () => {
    const onSubmit = vi.fn().mockRejectedValue(new Error('network'))
    const wrapper = mount(CallbackForm, { props: { onSubmit } })

    await wrapper.find('input[type="tel"]').setValue('+79991234567')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(wrapper.text()).toContain('Не удалось отправить')
  })
})
