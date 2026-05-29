<script setup lang="ts">
import type { PriceGroup } from '~/lib/prices'
import { formatPrice } from '~/lib/prices'
defineProps<{ groups: PriceGroup[] }>()
</script>

<template>
  <div class="space-y-10">
    <div v-for="g in groups" :key="g.category">
      <h2 class="text-2xl font-bold text-ink">{{ g.category }}</h2>
      <table class="mt-4 w-full border-collapse text-left">
        <tbody>
          <tr v-for="item in g.items" :key="item.id" class="border-b">
            <td class="py-3">
              <div class="font-medium text-ink">{{ item.name }}</div>
              <div v-if="item.description" class="text-sm text-muted">{{ item.description }}</div>
            </td>
            <td class="py-3 text-right font-medium text-brand whitespace-nowrap">{{ formatPrice(item) }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
