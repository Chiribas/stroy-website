<script setup lang="ts">
const api = useApi()
const { data } = await useAsyncData('home-portfolio', () => api.getArticles(1, 3))

const c = useContacts()
useSeoMeta({
  title: `${c.name} — ${c.tagline}`,
  description: 'Строительство, фасадные работы и ремонт под ключ. Прозрачные цены, гарантия 5 лет.',
  ogTitle: c.name,
  ogDescription: c.tagline,
})
</script>

<template>
  <div>
    <SectionHero />
    <SectionServicesTeaser />
    <SectionPricesTeaser />
    <SectionPortfolioTeaser :items="data?.items ?? []" />
    <SectionAbout />
    <SectionCta />
  </div>
</template>
