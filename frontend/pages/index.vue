<script setup lang="ts">
const api = useApi()
const { data } = await useAsyncData('home-portfolio', () => api.getArticles(1, 3))
const { data: services } = await useAsyncData('home-services', () => api.getServices())

const c = useContacts()
useSeoMeta({
  title: `${c.name} — ${c.tagline}`,
  description: 'Строительство, ремонт, замена фундамента и передвижка построек в Туле и области. Подскажем цену заранее, всё обсудим до начала.',
  ogTitle: c.name,
  ogDescription: c.tagline,
})
</script>

<template>
  <div>
    <SectionHero />
    <SectionServicesTeaser :services="services ?? []" />
    <SectionPricesTeaser />
    <SectionPortfolioTeaser :items="data?.items ?? []" />
    <SectionAbout />
    <SectionCta />
  </div>
</template>
