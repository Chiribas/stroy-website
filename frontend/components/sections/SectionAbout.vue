<script setup lang="ts">
const items = [
  { icon: 'badge-ruble', title: 'Примерная стоимость сразу', text: 'По фото и описанию подскажем порядок цены ещё до выезда.' },
  { icon: 'handshake', title: 'Всё обсудим заранее', text: 'Согласуем объём и стоимость до начала, по ходу работ честно корректируем.' },
  { icon: 'hammer', title: 'Полный цикл работ', text: 'Строительство, ремонт, замена фундамента, передвижка построек.' },
  { icon: 'truck', title: 'Материалы — наша забота', text: 'Под ключ: закупим и привезём по своей цене.' },
]
</script>

<template>
  <section class="bg-surface">
    <div class="mx-auto max-w-6xl px-4 py-16">
      <h2 class="text-3xl font-bold text-ink">Как работаем</h2>
      <div class="mt-8 grid gap-6 md:grid-cols-2 lg:grid-cols-4">
        <div v-for="i in items" :key="i.title" class="rounded-card border border-base bg-surface-2 p-6">
          <Icon :name="i.icon" class="h-7 w-7 text-brand" />
          <h3 class="mt-3 text-lg font-semibold text-ink">{{ i.title }}</h3>
          <p class="mt-1 text-muted">{{ i.text }}</p>
        </div>
      </div>
    </div>
  </section>
</template>
