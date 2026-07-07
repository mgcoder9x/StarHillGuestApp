<template>
  <div class="dashboard">
    <el-row :gutter="16">
      <el-col :span="6" v-for="card in cards" :key="card.label">
        <el-card shadow="hover">
          <div class="stat">
            <div class="stat-value">{{ card.value }}</div>
            <div class="stat-label">{{ card.label }}</div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <el-card shadow="never" class="note">
      <p>
        Khung admin (skeleton) — dựng lại sạch trên Vue 3.5 + TypeScript + Vite + Element Plus, tham chiếu
        layout Vuexy. Các màn nghiệp vụ (hộp thư, dọn phòng, nội quy…) sẽ nối vào API backend ở các lát cắt sau.
      </p>
    </el-card>
  </div>
</template>

<script setup lang="ts">
interface StatCard {
  label: string;
  value: string;
}

// Dữ liệu tĩnh placeholder — sẽ thay bằng gọi GET /api/admin/dashboard (đã có backend) ở lát cắt sau.
const cards: StatCard[] = [
  { label: 'Phòng đang hoạt động', value: '—' },
  { label: 'Ticket dọn phòng mở', value: '—' },
  { label: 'Hội thoại chưa đọc', value: '—' },
  { label: 'Khách đang truy cập', value: '—' },
];
</script>

<style scoped>
.stat {
  text-align: center;
  padding: 6px 0;
}
.stat-value {
  font-size: 28px;
  font-weight: 700;
  color: #0f766e;
}
.stat-label {
  color: #6b7a8d;
  font-size: 13px;
  margin-top: 4px;
}
.note {
  margin-top: 16px;
  color: #47566a;
}
</style>
