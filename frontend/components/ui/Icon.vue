<script setup lang="ts">
// Универсальная лайн-иконка: строковое имя → компонент lucide.
// Имена ключей используются в БД (Service.IconName) и в статических секциях.
import { Home, Hammer, Layers, Move, Key, Truck, Handshake, BadgeRussianRuble, Wrench } from 'lucide-vue-next'
const props = defineProps<{ name: string }>()
const map: Record<string, unknown> = {
  home: Home,
  hammer: Hammer,
  layers: Layers,
  move: Move,
  key: Key,
  truck: Truck,
  handshake: Handshake,
  'badge-ruble': BadgeRussianRuble,
  wrench: Wrench,
}
const cmp = computed(() => map[props.name] ?? Wrench)
</script>

<template>
  <component :is="cmp" />
</template>
