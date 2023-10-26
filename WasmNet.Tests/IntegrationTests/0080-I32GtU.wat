;; invoke: gtu
;; expect: (i32:0)

(module
    (memory 1)
    (func (export "gtu") (result i32)
        i32.const 42
        i32.const 42
        i32.gt_u
    )
)