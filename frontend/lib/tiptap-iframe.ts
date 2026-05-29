import { Node, mergeAttributes } from '@tiptap/core'

// Minimal block-level iframe node so Tiptap keeps <iframe> embeds (video players)
// instead of dropping them as unknown elements. The backend HtmlSanitizer still
// enforces the trusted-host whitelist on save.
export const Iframe = Node.create({
  name: 'iframe',
  group: 'block',
  atom: true,
  selectable: true,

  addAttributes() {
    return {
      src: { default: null },
      width: { default: '560' },
      height: { default: '315' },
      frameborder: { default: '0' },
      allowfullscreen: { default: 'true' },
      class: { default: 'video-embed' },
    }
  },

  parseHTML() {
    return [{ tag: 'iframe' }]
  },

  renderHTML({ HTMLAttributes }) {
    return ['iframe', mergeAttributes(HTMLAttributes)]
  },
})
