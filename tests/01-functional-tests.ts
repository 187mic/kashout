import { supabase } from '../src/lib/supabase';

console.log('🧪 FUNCTIONAL TESTS - Verification System\n');

interface TestCase {
  name: string;
  fn: () => Promise<void>;
}

const tests: TestCase[] = [];
let passCount = 0;
let failCount = 0;

function test(name: string, fn: () => Promise<void>) {
  tests.push({ name, fn });
}

async function runTests() {
  for (const t of tests) {
    try {
      process.stdout.write(`  ▶ ${t.name}... `);
      await t.fn();
      console.log('✅');
      passCount++;
    } catch (error) {
      console.log('❌');
      console.log(`    Error: ${error.message}`);
      failCount++;
    }
  }
  
  console.log(`\n📊 Results: ${passCount} passed, ${failCount} failed`);
}

// Test cases
test('User can submit verification with valid data', async () => {
  const email = `func.test.${Date.now()}@test.com`;
  const { data: user } = await supabase.auth.signUp({
    email,
    password: 'TestPass123!',
  });
  
  const { error } = await supabase.from('user_verifications').insert({
    user_id: user.user!.id,
    document_type: 'passport',
    document_number: 'TEST123',
    first_name: 'Test',
    last_name: 'User',
    date_of_birth: '1990-01-01',
    address_line1: '123 Test St',
    city: 'Test City',
    country: 'US',
    status: 'pending',
  });
  
  if (error) throw error;
});

test('Verification status transitions from pending to approved', async () => {
  const email = `transition.${Date.now()}@test.com`;
  const { data: user } = await supabase.auth.signUp({
    email,
    password: 'TestPass123!',
  });
  
  const { data: verification } = await supabase.from('user_verifications')
    .insert({
      user_id: user.user!.id,
      document_type: 'passport',
      document_number: 'TRANS123',
      first_name: 'Trans',
      last_name: 'Test',
      date_of_birth: '1990-01-01',
      address_line1: '123 Test',
      city: 'Test',
      country: 'US',
      status: 'pending',
    })
    .select()
    .single();
  
  const { data: updated } = await supabase.from('user_verifications')
    .update({ status: 'approved', verified_at: new Date().toISOString() })
    .eq('id', verification!.id)
    .select()
    .single();
  
  if (updated!.status !== 'approved') throw new Error('Status not updated');
});

test('User can resubmit after rejection', async () => {
  const email = `resubmit.${Date.now()}@test.com`;
  const { data: user } = await supabase.auth.signUp({
    email,
    password: 'TestPass123!',
  });
  
  const userId = user.user!.id;
  
  // First submission
  const { data: first } = await supabase.from('user_verifications')
    .insert({
      user_id: userId,
      document_type: 'passport',
      document_number: 'REJ123',
      first_name: 'Reject',
      last_name: 'Test',
      date_of_birth: '1990-01-01',
      address_line1: '123 Test',
      city: 'Test',
      country: 'US',
      status: 'pending',
    })
    .select()
    .single();
  
  // Reject it
  await supabase.from('user_verifications')
    .update({ status: 'rejected', notes: 'Test rejection' })
    .eq('id', first!.id);
  
  // Resubmit
  const { error } = await supabase.from('user_verifications')
    .insert({
      user_id: userId,
      document_type: 'passport',
      document_number: 'REJ456',
      first_name: 'Reject',
      last_name: 'Test',
      date_of_birth: '1990-01-01',
      address_line1: '123 Test',
      city: 'Test',
      country: 'US',
      status: 'pending',
    });
  
  if (error) throw error;
});

runTests();