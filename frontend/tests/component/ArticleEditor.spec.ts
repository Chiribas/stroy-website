import { describe, it, expect, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import ArticleEditor from '~/components/admin/ArticleEditor.vue'

// MediaUploader pulls in useAdminApi (Nuxt runtime) — stub it out for this unit test.
vi.mock('~/components/admin/MediaUploader.vue', () => ({
  default: { name: 'MediaUploader', template: '<span class="media-uploader-stub" />' },
}))

describe('ArticleEditor', () => {
  it('mounts and renders the toolbar with formatting buttons', async () => {
    const wrapper = mount(ArticleEditor, {
      props: { modelValue: '<p>hello</p>' },
    })
    // useEditor initialises the editor on mount; wait a tick for the toolbar v-if.
    await flushPromises()
    await new Promise(r => setTimeout(r, 0))

    expect(wrapper.text()).toContain('Жирный')
    wrapper.unmount()
  })
})
